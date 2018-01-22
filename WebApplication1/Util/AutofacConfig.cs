using Autofac;
using Autofac.Integration.Mvc;
using System.Web.Mvc;
using WebApplication1.Repository;

namespace WebApplication1.Util
{
    public class AutofacConfig
    {
        public static void ConfigureContainer()
        {
            var builder = new ContainerBuilder();

            builder.RegisterControllers(typeof(MvcApplication).Assembly);

            builder.RegisterType<MyRepository>().As<IRepository>();

            var container = builder.Build();

            DependencyResolver.SetResolver(new AutofacDependencyResolver(container));
        }
    }
}