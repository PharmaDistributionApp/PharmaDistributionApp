using System.Windows;
using Microsoft.EntityFrameworkCore;
using PharmaDistributionApp.Models;

namespace PharmaDistributionApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // TỰ ĐỘNG TẠO FILE DB NẾU CHƯA CÓ
            using (var context = new QuanlyphanphoiduocphamContext())
            {
                context.Database.EnsureCreated();
            }
        }
    }
}