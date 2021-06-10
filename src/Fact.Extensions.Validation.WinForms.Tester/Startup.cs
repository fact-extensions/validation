using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fact.Extensions.Validation.WinForms.Tester
{
    public static class Startup
    {
        public static void ConfigureServices(IServiceCollection sc)
        {
            sc.AddSingleton<StyleManager>();
        }


        public static void Configure(IServiceProvider services)
        {

        }
    }
}
