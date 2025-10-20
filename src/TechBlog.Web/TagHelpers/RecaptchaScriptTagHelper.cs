using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using TechBlog.Core.Interfaces.Services;

namespace TechBlog.Web.TagHelpers
{
    [HtmlTargetElement("recaptcha-script")]
    public class RecaptchaScriptTagHelper : TagHelper
    {
        private readonly IRecaptchaService _recaptchaService;
        private readonly IConfiguration _configuration;

        public string Action { get; set; } = "submit";

        public RecaptchaScriptTagHelper(IRecaptchaService recaptchaService, IConfiguration configuration)
        {
            _recaptchaService = recaptchaService;
            _configuration = configuration;
        }

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {
            output.TagName = null;
            
            var settings = await _recaptchaService.GetSettingsAsync();
            if (!settings.IsEnabled)
                return;

            var siteKey = settings.SiteKey ?? _configuration["Recaptcha:SiteKey"];
            if (string.IsNullOrEmpty(siteKey))
                return;

            var script = $@"
                <script>
                    // This function will be called when reCAPTCHA loads
                    function onRecaptchaLoad() {{
                        console.log('reCAPTCHA loaded');
                        // Handle any pending form submissions
                        if (window.pendingRecaptchaSubmission) {{
                            window.pendingRecaptchaSubmission();
                            window.pendingRecaptchaSubmission = null;
                        }}
                    }}
                </script>
                <script src='https://www.google.com/recaptcha/api.js?onload=onRecaptchaLoad&render={siteKey}' async defer></script>
                <script>
                    function setupRecaptcha() {{
                        if (typeof grecaptcha === 'undefined') {{
                            // If reCAPTCHA isn't loaded yet, try again in 500ms
                            setTimeout(setupRecaptcha, 500);
                            return;
                        }}

                        document.querySelectorAll('form[data-recaptcha]').forEach(form => {{
                            // Only add the event listener once
                            if (form.hasAttribute('data-recaptcha-initialized')) {{
                                return;
                            }}
                            form.setAttribute('data-recaptcha-initialized', 'true');
                            
                            form.addEventListener('submit', function(e) {{
                                e.preventDefault();
                                const form = this;
                                
                                // If reCAPTCHA is ready, execute it
                                if (typeof grecaptcha !== 'undefined' && typeof grecaptcha.execute === 'function') {{
                                    grecaptcha.execute('{siteKey}', {{action: '{Action}'}})
                                        .then(function(token) {{
                                            // Remove any existing token
                                            const existingInput = form.querySelector('input[name=""g-recaptcha-response""]');
                                            if (existingInput) {{
                                                existingInput.remove();
                                            }}
                                            
                                            // Add the new token
                                            const input = document.createElement('input');
                                            input.type = 'hidden';
                                            input.name = 'g-recaptcha-response';
                                            input.value = token;
                                            form.appendChild(input);
                                            
                                            // Submit the form
                                            form.submit();
                                        }})
                                        .catch(function(error) {{
                                            console.error('reCAPTCHA error:', error);
                                            // Fallback: submit the form anyway if reCAPTCHA fails
                                            form.submit();
                                        }});
                                }} else {{
                                    // Fallback: submit the form if reCAPTCHA isn't available
                                    console.warn('reCAPTCHA not available, submitting form without verification');
                                    form.submit();
                                }}
                            }});
                        }});
                    }}

                    // Start the setup when the DOM is fully loaded
                    if (document.readyState === 'loading') {{
                        document.addEventListener('DOMContentLoaded', setupRecaptcha);
                    }} else {{
                        setupRecaptcha();
                    }}
                </script>";

            output.Content.SetHtmlContent(script);
        }
    }
}
