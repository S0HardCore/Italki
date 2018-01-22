using Dapper;
using Newtonsoft.Json;
using System;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using WebApplication1.Models;

namespace WebApplication1.Repository
{
    interface IRepository
    {

    }

    public class MyRepository : IRepository
    {
        public MyRepository()
        {

        }

        public string GetConnectionString() =>
            System.Configuration.ConfigurationManager.ConnectionStrings["connectionString"].ConnectionString;
        public string GetDataBaseUrl() =>
            "https://www.italki.com/api/teachersv2?_r=1516618599088&i_token=TkRnd01qY3hNQT09fDE1MTY2MTg1ODZ8MmU5NDEwZTNiMWU3NDcwZTQ3NjIxYzA5Y2EzYmIwMDU0NTE5OTM0NA%3D%3D&page=";

        public object GetTeacher(int id)
        {
            using (var sqlC = GetConnection())
            {
                string sql = @"SELECT t.ID,
		                              t.Name,
		                              t.Rating,
		                              t.Students,
		                              t.Lessons,
		                              t.Description,
		                              t.Price,
		                              t.Country
                                 FROM Italki.dbo.Teachers t
                                WHERE t.ID = @TID

                               SELECT l.LangID,
                                      l.LanguageName,
		                              l.Skill
                                 FROM Italki.dbo.Languages l
		                              LEFT JOIN Italki.dbo.TeachersLanguages tl ON tl.LanguageID = l.LangID
                                      LEFT JOIN Italki.dbo.Teachers t ON t.ID = tl.TeacherID
                                WHERE t.ID = @TID

                               SELECT tag.ID,
	                                  tag.TagName
                                 FROM Italki.dbo.Tags tag
		                              LEFT JOIN Italki.dbo.TeachersTags tt ON tt.TagID = tag.ID
                                      LEFT JOIN Italki.dbo.Teachers t ON t.ID = tt.TeacherID
                                WHERE t.ID = @TID";

                using (var multi = sqlC.QueryMultiple(sql, new { TID = id }))
                {
                    var teacher = multi.Read<Teacher>().First();
                    teacher.Languages = multi.Read<Language>().OrderByDescending(l => l.Skill).ToList();
                    teacher.Tags = multi.Read<Tag>().ToList();

                    return teacher;
                }
            }
        }

        public object GetTeachers(int pageIndex = 1, int pageSize = 20, string sortField = "Name", string sortOrder = "asc")
        {
            if (string.IsNullOrEmpty(sortField))
                sortField = "Name";
            using (var sqlC = GetConnection())
            {
                string sql = $@"SELECT t.ID,
                                       t.Name,
                                       t.Rating,
                                       t.Students,
                                       t.Lessons,
                                       t.Description,
                                       t.Price,
                                       t.Country,
                                       COUNT(1) OVER() as itemsCount
                                  FROM Italki.dbo.Teachers t
                              ORDER BY t.{sortField} {sortOrder}
                                OFFSET {(pageIndex - 1) * pageSize} ROWS 
                            FETCH NEXT {pageSize} ROWS ONLY;";

                PagedTeacher pt = new PagedTeacher();
                sqlC.Query<Teacher, int, PagedTeacher>(
                    sql,
                    (t, i) =>
                    {
                        pt.data.Add(t);
                        if (pt.itemsCount == 0)
                            pt.itemsCount = i;
                        return pt;
                    },
                    splitOn: "itemsCount");

                return pt;
            }
        }

        //public void ClearDB()
        //{
        //    using (var sqlC = GetConnection())
        //    {
        //        string sql = @"DELETE FROM TeachersLanguages;
        //                       DELETE FROM TeachersTags;
        //                       DELETE FROM Languages;
        //                       DELETE FROM Tags;
        //                       DELETE FROM Teachers;";
        //        sqlC.Execute(sql);
        //    }
        //}

        public void RefreshDB(int pages)
        {
            //ClearDB();
            int totalPages = pages;
            using (var sqlC = GetConnection())
            {
                for (int page = 1; page <= totalPages; ++page)
                {
                    if (page % 2 == 0)
                        Thread.Sleep(2000);
                    string response = Request(GetDataBaseUrl() + page.ToString());
                    try
                    {
                        var json = JsonConvert.DeserializeObject<DataBaseResponse>(response);
                        int totalPagesFromResponse = json.meta.statistics_info.count / 20;
                        if (pages == 256 || pages > totalPagesFromResponse)
                            totalPages = totalPagesFromResponse;
                        foreach (var teacher in json.data)
                        {
                            try
                            {
                                teacher.origin_country_id = new RegionInfo(teacher.origin_country_id).DisplayName;
                            }
                            catch (ArgumentException)
                            {
                                teacher.origin_country_id = "Unknown";
                            }
                            foreach (var tag in teacher.personal_tag)
                            {
                                try
                                {
                                    string sql = @"IF EXISTS(SELECT 1
		                                                       FROM Italki.dbo.Tags
			                                                   WITH (NOLOCK)
		                                                      WHERE ID = @ID)
		                                                  BEGIN
			                                                 UPDATE Italki.dbo.Tags
			                                                    SET TagName=@TagName
			                                                  WHERE ID = @ID
		                                                  END
	                                                  ELSE
		                                                  BEGIN
			                                            INSERT INTO Italki.dbo.Tags(ID,
									                                                TagName)
								                                             VALUES(@ID,
								                                                    @TagName)
		                                                  END";
                                    sqlC.Execute(sql, new Tag
                                    {
                                        ID = tag.tag_id,
                                        TagName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(tag.tag_name)
                                    });
                                }
                                catch (Exception) { }
                            }
                            try
                            {
                                string sql = @"IF EXISTS(SELECT 1
		                                                   FROM Italki.dbo.Teachers
			                                               WITH (NOLOCK)
		                                                   WHERE ID = @ID)
		                                               BEGIN
			                                             UPDATE Italki.dbo.Teachers
			                                                SET Name=@Name,
                                                                Rating=@Rating,
                                                                Students=@Students,
                                                                Lessons=@Lessons,
                                                                Description=@Description,
                                                                Price=@Price,
                                                                Country=@Country
			                                               WHERE ID = @ID
		                                               END
	                                               ELSE
		                                               BEGIN
			                                            INSERT INTO Teachers(ID,
                                                                            Name,
                                                                            Rating,
                                                                            Students,
                                                                            Lessons,
                                                                            Description,
                                                                            Price,
                                                                            Country) 
                                                                     VALUES(@ID,
                                                                            @Name,
                                                                            @Rating,
                                                                            @Students,
                                                                            @Lessons,
                                                                            @Description,
                                                                            @Price,
                                                                            @Country)
                                                       END";
                                sqlC.Execute(sql, new Teacher
                                {
                                    ID = teacher.id,
                                    Name = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(teacher.nickname.Trim()),
                                    Rating = teacher.teacher_info_obj.pro_rating,
                                    Students = teacher.teacher_info_obj.student_count,
                                    Lessons = teacher.teacher_info_obj.session_count,
                                    Description = teacher.teacher_info_obj.intro.Trim(),
                                    Price = teacher.teacher_info_obj.min_price_usd,
                                    Country = teacher.origin_country_id.Trim()
                                });
                            }
                            catch (Exception) { }
                            foreach (var tag in teacher.personal_tag)
                            {
                                try
                                {
                                    string sql = @"IF NOT EXISTS(SELECT 1
                                                                   FROM Italki.dbo.TeachersTags
                                                                   WITH (NOLOCK)
                                                                  WHERE TeacherID = @TeacherID AND
                                                                        TagID = @TagID)
                                                            BEGIN
                                                INSERT INTO TeachersTags(TeacherID, 
                                                                         TagID) 
                                                                  VALUES(@TeacherID,
                                                                         @TagID)
                                                            END";

                                    sqlC.Execute(sql, new TeachersTags
                                    {
                                        TeacherID = teacher.id,
                                        TagID = tag.tag_id
                                    });
                                }
                                catch (Exception) { }
                            }
                            foreach (var lang in teacher.language_obj_s)
                            {
                                try
                                {
                                    string sql = @"IF EXISTS(SELECT 1
		                                                       FROM Italki.dbo.Languages
			                                                   WITH (NOLOCK)
		                                                      WHERE LangID = @LangID)
		                                                   BEGIN
			                                                 UPDATE Italki.dbo.Languages
			                                                    SET LanguageName=@LanguageName,
			                                                        Skill=@Skill
			                                                  WHERE LangID = @LangID
		                                                   END
	                                                   ELSE
		                                                   BEGIN
			                                            INSERT INTO Italki.dbo.Languages(LangID,
									                                                    LanguageName,
											                                            Skill)
									                                             VALUES(@LangID,
											                                            @LanguageName,
											                                            @Skill)
		                                                   END";
                                    sqlC.Execute(sql, new Language
                                    {
                                        LangID = lang.id,
                                        LanguageName = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(lang.language),
                                        Skill = lang.level
                                    });

                                    sql = @"IF NOT EXISTS(SELECT 1
                                                            FROM Italki.dbo.TeachersLanguages
                                                            WITH (NOLOCK)
                                                           WHERE TeacherID = @TeacherID AND
                                                                 LanguageID = @LanguageID)
                                                      BEGIN
                                                     INSERT INTO TeachersLanguages(TeacherID, 
                                                                                   LanguageID) 
                                                                            VALUES(@TeacherID,
                                                                                   @LanguageID)
                                                     END";
                                    sqlC.Execute(sql, new TeachersLanguages
                                    {
                                        TeacherID = teacher.id,
                                        LanguageID = lang.id
                                    });
                                }
                                catch (Exception) { }
                            }
                            teacher.ratio = (float)teacher.teacher_info_obj.session_count / teacher.teacher_info_obj.student_count;
                        }
                    }
                    catch (NullReferenceException) { break; } // End of pages or any response error
                }
            }
        }

        public SqlConnection GetConnection()
        {
            var sqlC = new SqlConnection(GetConnectionString());
            sqlC.Open();
            return sqlC;
        }

        public string Request(string url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
            }
            catch (WebException we)
            {
                response = (HttpWebResponse)we.Response;
            }
            return new StreamReader(response.GetResponseStream()).ReadToEnd();
        }
    }
}