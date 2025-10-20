using System.Threading.Tasks;
using TechBlog.Core.Entities;

namespace TechBlog.Core.Interfaces.Services
{
    public interface IRecaptchaService
    {
        Task<bool> VerifyCaptchaAsync(string token, string action);
        Task<RecaptchaSettings> GetSettingsAsync();
        Task UpdateSettingsAsync(RecaptchaSettings settings);
    }
}
