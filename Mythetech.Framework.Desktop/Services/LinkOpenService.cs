using System.Diagnostics;
using Mythetech.Framework.Infrastructure;

namespace Mythetech.Framework.Desktop.Services
{
    public class LinkOpenService : ILinkOpenService
    {
        public async Task OpenLinkAsync(string url)
        {
            await Task.Run(() => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }));
        }
    }
}