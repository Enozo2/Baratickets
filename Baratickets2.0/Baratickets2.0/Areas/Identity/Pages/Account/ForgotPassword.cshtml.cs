// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using Baratickets2._0.Models;
using Baratickets2._0.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Baratickets2._0.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IEmailService _emailService;

        public ForgotPasswordModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender, IEmailService emailService)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _emailService = emailService;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///     directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(Input.Email);
                if (user == null) return RedirectToPage("./ForgotPasswordConfirmation");

                // Generamos el código y el link
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Page(
                    "/Account/ResetPassword",
                    pageHandler: null,
                    values: new { area = "Identity", code },
                    protocol: Request.Scheme);

                // PEGA AQUÍ TU MENSAJE
                string mensaje = $@"
            <div style='font-family: Arial; text-align: center; border: 1px solid #ddd; padding: 30px; border-radius: 15px;'>
                <h2 style='color: #0d6efd;'>Restablecer Contraseña</h2>
                <p>Has solicitado recuperar tu acceso a <b>BaraTickets 2.0</b>.</p>
                <p>Haz clic en el botón de abajo para elegir una nueva clave:</p>
                <br>
                <a href='{callbackUrl}' style='background-color: #0d6efd; color: white; padding: 12px 25px; text-decoration: none; border-radius: 10px; font-weight: bold;'>Restablecer mi Contraseña</a>
                <br><br>
                <p style='color: #666; font-size: 12px;'>Si no solicitaste este cambio, puedes ignorar este correo de forma segura.</p>
            </div>";

                // Enviamos el correo (esto limpiará la raya roja de la imagen 4f67bd)
                await _emailService.EnviarCorreoAsync(Input.Email, "Recuperar Contraseña - BaraTickets 2.0", mensaje);

                return RedirectToPage("./ForgotPasswordConfirmation");
            }
            return Page();
        }
    }
}
