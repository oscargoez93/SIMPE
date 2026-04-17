using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace SIMPE.Dashboard.Services
{
    public class GraphEmailService
    {
        private readonly string _tenantId = "646ad0fd-3eb3-4066-9b7d-15c920eb58f1";
        private readonly string _clientId = "e2989cc0-8035-4cb5-a9bf-989a1c51a6b3";
        private readonly string _clientSecret = "CaQ8Q~J-texk6yov8z.9Nk~NQBGWo8l0NyvvLbHN";
        private readonly string _senderEmail = "liderti@visiongerencial.com.co";
        private readonly string _adminEmail = "oscar.goezb@comunidad.iush.edu.co";

        public async Task SendApprovalEmailAsync(Models.User user)
        {
            var scopes = new[] { "https://graph.microsoft.com/.default" };
            
            var clientSecretCredential = new ClientSecretCredential(_tenantId, _clientId, _clientSecret);
            var graphClient = new GraphServiceClient(clientSecretCredential, scopes);

            var message = new Message
            {
                Subject = $"Aprobación requerida SIMPE Dashboard - {user.FullName}",
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = $@"
                        <h2>Nueva solicitud de registro</h2>
                        <p>El siguiente usuario ha solicitado acceso al Dashboard de SIMPE:</p>
                        <ul>
                            <li><strong>Nombre:</strong> {user.FullName}</li>
                            <li><strong>Correo:</strong> {user.Email}</li>
                        </ul>
                        <p>Para aprobar este usuario, haz clic en el siguiente enlace de aprobación (requiere que el servidor esté encendido en tu PC local):</p>
                        <p><a href='http://localhost:5240/api/auth/approve/{user.Id}' style='display:inline-block;padding:10px 15px;background-color:#F47920;color:white;text-decoration:none;border-radius:5px;font-weight:bold;'>Aprobar Usuario</a></p>
                    "
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient { EmailAddress = new EmailAddress { Address = _adminEmail } }
                }
            };

            var saveToSentItems = false;
            
            try
            {
                await graphClient.Users[_senderEmail]
                    .SendMail
                    .PostAsync(new Microsoft.Graph.Users.Item.SendMail.SendMailPostRequestBody
                    {
                        Message = message,
                        SaveToSentItems = saveToSentItems
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar correo Graph API: {ex.Message}");
            }
        }
    }
}
