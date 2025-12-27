using System.Diagnostics;
using Mythetech.Components.Infrastructure;

namespace Mythetech.Components.Desktop.Services
{
    public class LinkOpenService : ILinkOpenService
    {
        public async Task OpenLinkAsync(string url)
        {
            await Task.Run(() => Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }));
        }
    }
}