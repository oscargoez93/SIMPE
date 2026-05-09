using System.Diagnostics;
using System.Management;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
using SIMPE.Agent.Models;

namespace SIMPE.Agent.Services
{
    public class SecurityCollectorService
    {
        public SecurityScanResult GatherSecurityInfo()
        {
            var result = new SecurityScanResult
            {
                generatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                items = new List<SecurityScanItem>
                {
                    GetAntivirusAndThreatProtection(),
                    GetAccountProtection(),
                    GetFirewallProtection(),
                    GetAppAndBrowserControl(),
                    GetDeviceSecurity(),
                    GetProtectionHistory(),
                    GetCoreIsolation(),
                    GetSecurityProcessor(),
                    GetSecureBoot(),
                    GetDataEncryption()
                }
            };

            result.overallStatus = ResolveOverallStatus(result.items);
            return result;
        }

        private SecurityScanItem GetAntivirusAndThreatProtection()
        {
            var details = new List<SecurityScanDetail>();
            var antivirusProducts = GetWmiValues(@"root\SecurityCenter2", "SELECT displayName, productState FROM AntiVirusProduct");
            foreach (var product in antivirusProducts)
            {
                details.Add(Detail("Producto", product.GetValueOrDefault("displayName", "No detectado")));
                details.Add(Detail("Estado del producto", DecodeSecurityCenterProductState(product.GetValueOrDefault("productState", ""))));
            }

            var defenderStatus = GetPowerShellJson("Get-MpComputerStatus | Select-Object AMServiceEnabled,AntivirusEnabled,AntispywareEnabled,RealTimeProtectionEnabled,IoavProtectionEnabled,NISEnabled,AntivirusSignatureLastUpdated,QuickScanAge,FullScanAge | ConvertTo-Json -Compress");
            var antivirusEnabled = ReadJsonBool(defenderStatus, "AntivirusEnabled");
            var realTimeEnabled = ReadJsonBool(defenderStatus, "RealTimeProtectionEnabled");

            AddJsonDetail(details, defenderStatus, "AMServiceEnabled", "Servicio antimalware");
            AddJsonDetail(details, defenderStatus, "AntivirusEnabled", "Antivirus activo");
            AddJsonDetail(details, defenderStatus, "RealTimeProtectionEnabled", "Proteccion en tiempo real");
            AddJsonDetail(details, defenderStatus, "IoavProtectionEnabled", "Analisis de archivos descargados");
            AddJsonDetail(details, defenderStatus, "AntivirusSignatureLastUpdated", "Ultima firma");
            AddJsonDetail(details, defenderStatus, "QuickScanAge", "Dias desde escaneo rapido");
            AddJsonDetail(details, defenderStatus, "FullScanAge", "Dias desde escaneo completo");

            var ok = antivirusProducts.Count > 0 && antivirusEnabled != false && realTimeEnabled != false;
            return Item(
                "antivirus-threats",
                "Proteccion antivirus y contra amenazas",
                ok ? "ok" : "warning",
                ok ? "Antivirus detectado y proteccion en tiempo real activa." : "Revisar antivirus o proteccion en tiempo real.",
                details);
        }

        private SecurityScanItem GetAccountProtection()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            var isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            var uacValue = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\System", "EnableLUA", null)?.ToString();
            var helloValue = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\PolicyManager\default\Settings\AllowSignInOptions", "value", null)?.ToString();

            var details = new List<SecurityScanDetail>
            {
                Detail("Usuario", identity.Name),
                Detail("Administrador local", isAdmin ? "Si" : "No"),
                Detail("UAC", uacValue == "1" ? "Activado" : "Desactivado o no detectado"),
                Detail("Windows Hello / opciones de inicio", helloValue == "1" ? "Permitido" : "No detectado")
            };

            return Item(
                "account-protection",
                "Proteccion de cuentas",
                uacValue == "1" ? "ok" : "warning",
                uacValue == "1" ? "Control de cuentas de usuario activo." : "UAC no aparece activo.",
                details);
        }

        private SecurityScanItem GetFirewallProtection()
        {
            var profiles = new[]
            {
                ("Dominio", @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\DomainProfile"),
                ("Privado", @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\StandardProfile"),
                ("Publico", @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy\PublicProfile")
            };

            var details = new List<SecurityScanDetail>();
            var enabledProfiles = 0;
            foreach (var profile in profiles)
            {
                var enabled = Registry.GetValue(profile.Item2, "EnableFirewall", null)?.ToString();
                if (enabled == "1")
                {
                    enabledProfiles++;
                }
                details.Add(Detail($"Perfil {profile.Item1}", enabled == "1" ? "Firewall activado" : "Firewall desactivado o no detectado"));
            }

            var status = enabledProfiles == profiles.Length ? "ok" : enabledProfiles > 0 ? "warning" : "danger";
            return Item(
                "firewall-network",
                "Firewall y proteccion de red",
                status,
                $"{enabledProfiles} de {profiles.Length} perfiles con firewall activo.",
                details);
        }

        private SecurityScanItem GetAppAndBrowserControl()
        {
            var explorerSmartScreen = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", "SmartScreenEnabled", null)?.ToString()
                ?? Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer", "SmartScreenEnabled", null)?.ToString();
            var edgeSmartScreen = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Policies\Microsoft\Edge", "SmartScreenEnabled", null)?.ToString()
                ?? Registry.GetValue(@"HKEY_CURRENT_USER\SOFTWARE\Policies\Microsoft\Edge", "SmartScreenEnabled", null)?.ToString();
            var defenderPrefs = GetPowerShellJson("Get-MpPreference | Select-Object PUAProtection,EnableControlledFolderAccess,EnableNetworkProtection | ConvertTo-Json -Compress");

            var details = new List<SecurityScanDetail>
            {
                Detail("SmartScreen de Windows", string.IsNullOrWhiteSpace(explorerSmartScreen) ? "No detectado" : explorerSmartScreen),
                Detail("SmartScreen de Microsoft Edge", edgeSmartScreen == "1" ? "Activado por politica" : edgeSmartScreen == "0" ? "Desactivado por politica" : "Sin politica detectada")
            };
            AddJsonDetail(details, defenderPrefs, "PUAProtection", "Bloqueo de apps potencialmente no deseadas");
            AddJsonDetail(details, defenderPrefs, "EnableControlledFolderAccess", "Acceso controlado a carpetas");
            AddJsonDetail(details, defenderPrefs, "EnableNetworkProtection", "Proteccion de red");

            var smartScreenOk = explorerSmartScreen is "RequireAdmin" or "Warn" or "Block" || edgeSmartScreen == "1";
            return Item(
                "app-browser-control",
                "Control de aplicaciones y navegador",
                smartScreenOk ? "ok" : "warning",
                smartScreenOk ? "SmartScreen o politicas de navegador detectadas." : "No se detecto una configuracion fuerte de SmartScreen.",
                details);
        }

        private SecurityScanItem GetDeviceSecurity()
        {
            var coreIsolation = GetCoreIsolation();
            var tpm = GetSecurityProcessor();
            var secureBoot = GetSecureBoot();
            var encryption = GetDataEncryption();
            var details = new List<SecurityScanDetail>
            {
                Detail("Aislamiento del nucleo", coreIsolation.summary),
                Detail("Procesador de seguridad", tpm.summary),
                Detail("Arranque seguro", secureBoot.summary),
                Detail("Cifrado de datos", encryption.summary)
            };

            var status = ResolveOverallStatus(new[] { coreIsolation, tpm, secureBoot, encryption });
            return Item(
                "device-security",
                "Seguridad del dispositivo",
                status,
                status == "ok" ? "Caracteristicas principales de seguridad de hardware activas." : "Hay caracteristicas de seguridad de hardware por revisar.",
                details);
        }

        private SecurityScanItem GetProtectionHistory()
        {
            var history = GetPowerShellJson("Get-WinEvent -LogName 'Microsoft-Windows-Windows Defender/Operational' -MaxEvents 15 -ErrorAction SilentlyContinue | Select-Object TimeCreated,Id,LevelDisplayName,Message | ConvertTo-Json -Compress");
            var details = new List<SecurityScanDetail>();
            var eventCount = 0;

            try
            {
                if (!string.IsNullOrWhiteSpace(history))
                {
                    using var doc = JsonDocument.Parse(history);
                    if (doc.RootElement.ValueKind == JsonValueKind.Array)
                    {
                        eventCount = doc.RootElement.GetArrayLength();
                        var first = doc.RootElement.EnumerateArray().FirstOrDefault();
                        AddEventDetails(details, first);
                    }
                    else if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    {
                        eventCount = 1;
                        AddEventDetails(details, doc.RootElement);
                    }
                }
            }
            catch
            {
                details.Add(Detail("Historial", "No fue posible interpretar eventos recientes."));
            }

            details.Insert(0, Detail("Eventos recientes de Defender", eventCount.ToString()));
            return Item(
                "protection-history",
                "Historial de proteccion",
                eventCount > 0 ? "ok" : "unknown",
                eventCount > 0 ? "Eventos recientes consultados correctamente." : "No se encontraron eventos recientes o no hay permisos para leerlos.",
                details);
        }

        private SecurityScanItem GetCoreIsolation()
        {
            var hvci = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard\Scenarios\HypervisorEnforcedCodeIntegrity", "Enabled", null)?.ToString();
            var vbs = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard", "EnableVirtualizationBasedSecurity", null)?.ToString();
            var requirePlatformSecurity = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\DeviceGuard", "RequirePlatformSecurityFeatures", null)?.ToString();

            var details = new List<SecurityScanDetail>
            {
                Detail("Integridad de memoria (HVCI)", hvci == "1" ? "Activada" : "Desactivada o no detectada"),
                Detail("Seguridad basada en virtualizacion", vbs == "1" ? "Activada" : "Desactivada o no detectada"),
                Detail("Requisitos de plataforma", string.IsNullOrWhiteSpace(requirePlatformSecurity) ? "No detectado" : requirePlatformSecurity)
            };

            return Item(
                "core-isolation",
                "Aislamiento del nucleo",
                hvci == "1" ? "ok" : "warning",
                hvci == "1" ? "Integridad de memoria activada." : "Integridad de memoria no aparece activada.",
                details);
        }

        private SecurityScanItem GetSecurityProcessor()
        {
            var details = new List<SecurityScanDetail>();
            var tpmValues = GetWmiValues(@"root\CIMV2\Security\MicrosoftTpm", "SELECT * FROM Win32_Tpm").FirstOrDefault() ?? new Dictionary<string, string>();
            if (tpmValues.Count == 0)
            {
                details.Add(Detail("TPM", "No detectado"));
                return Item("security-processor", "Procesador de seguridad", "warning", "No se detecto TPM.", details);
            }

            details.Add(Detail("TPM habilitado", BoolText(tpmValues.GetValueOrDefault("IsEnabled_InitialValue", ""))));
            details.Add(Detail("TPM activado", BoolText(tpmValues.GetValueOrDefault("IsActivated_InitialValue", ""))));
            details.Add(Detail("TPM propietario", BoolText(tpmValues.GetValueOrDefault("IsOwned_InitialValue", ""))));
            details.Add(Detail("Version TPM", tpmValues.GetValueOrDefault("SpecVersion", "No detectado")));
            details.Add(Detail("Fabricante", tpmValues.GetValueOrDefault("ManufacturerVersion", "No detectado")));

            var enabled = tpmValues.GetValueOrDefault("IsEnabled_InitialValue", "").Equals("True", StringComparison.OrdinalIgnoreCase);
            return Item(
                "security-processor",
                "Procesador de seguridad",
                enabled ? "ok" : "warning",
                enabled ? "TPM detectado y habilitado." : "TPM detectado pero no habilitado.",
                details);
        }

        private SecurityScanItem GetSecureBoot()
        {
            var secureBootValue = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecureBoot\State", "UEFISecureBootEnabled", null)?.ToString();
            var details = new List<SecurityScanDetail>
            {
                Detail("Arranque seguro UEFI", secureBootValue == "1" ? "Activado" : secureBootValue == "0" ? "Desactivado" : "No detectado")
            };

            return Item(
                "secure-boot",
                "Arranque seguro",
                secureBootValue == "1" ? "ok" : "warning",
                secureBootValue == "1" ? "Arranque seguro activado." : "Arranque seguro no aparece activado.",
                details);
        }

        private SecurityScanItem GetDataEncryption()
        {
            var volumes = GetWmiValues(@"root\CIMV2\Security\MicrosoftVolumeEncryption", "SELECT DriveLetter, ProtectionStatus, ConversionStatus, EncryptionMethod FROM Win32_EncryptableVolume");
            var details = new List<SecurityScanDetail>();
            var protectedVolumes = 0;

            foreach (var volume in volumes.Where(v => !string.IsNullOrWhiteSpace(v.GetValueOrDefault("DriveLetter", ""))))
            {
                var drive = volume.GetValueOrDefault("DriveLetter", "Volumen");
                var protection = DecodeBitLockerProtection(volume.GetValueOrDefault("ProtectionStatus", ""));
                var conversion = DecodeBitLockerConversion(volume.GetValueOrDefault("ConversionStatus", ""));
                details.Add(Detail(drive, $"{protection} / {conversion}"));
                if (volume.GetValueOrDefault("ProtectionStatus", "") == "1")
                {
                    protectedVolumes++;
                }
            }

            if (details.Count == 0)
            {
                details.Add(Detail("BitLocker", "No se detectaron volumenes cifrables."));
            }

            return Item(
                "data-encryption",
                "Cifrado de datos",
                protectedVolumes > 0 ? "ok" : "warning",
                protectedVolumes > 0 ? $"{protectedVolumes} volumen(es) con proteccion activa." : "No se detecto proteccion BitLocker activa.",
                details);
        }

        private List<Dictionary<string, string>> GetWmiValues(string scope, string query)
        {
            var values = new List<Dictionary<string, string>>();
            if (!OperatingSystem.IsWindows())
            {
                return values;
            }

            try
            {
                using var searcher = new ManagementObjectSearcher(scope, query);
                foreach (ManagementObject obj in searcher.Get())
                {
                    var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (PropertyData property in obj.Properties)
                    {
                        row[property.Name] = property.Value?.ToString() ?? "";
                    }
                    values.Add(row);
                }
            }
            catch
            {
                // Some namespaces require elevation or are unavailable on specific Windows editions.
            }

            return values;
        }

        private string GetPowerShellJson(string command)
        {
            if (!OperatingSystem.IsWindows())
            {
                return "";
            }

            try
            {
                using var process = new Process();
                process.StartInfo = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -ExecutionPolicy Bypass -Command \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                process.Start();
                if (!process.WaitForExit(8000))
                {
                    process.Kill(true);
                    return "";
                }

                var output = process.StandardOutput.ReadToEnd();
                return output.Trim();
            }
            catch
            {
                return "";
            }
        }

        private static void AddJsonDetail(List<SecurityScanDetail> details, string json, string property, string label)
        {
            var value = ReadJsonString(json, property);
            if (!string.IsNullOrWhiteSpace(value))
            {
                details.Add(Detail(label, value));
            }
        }

        private static bool? ReadJsonBool(string json, string property)
        {
            if (TryGetJsonProperty(json, property, out var element) && (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False))
            {
                return element.GetBoolean();
            }

            return null;
        }

        private static string ReadJsonString(string json, string property)
        {
            if (!TryGetJsonProperty(json, property, out var element))
            {
                return "";
            }

            return element.ValueKind switch
            {
                JsonValueKind.True => "Activado",
                JsonValueKind.False => "Desactivado",
                JsonValueKind.Number => element.ToString(),
                JsonValueKind.String => element.GetString() ?? "",
                _ => element.ToString()
            };
        }

        private static bool TryGetJsonProperty(string json, string property, out JsonElement element)
        {
            element = default;
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty(property, out var found))
                {
                    element = found.Clone();
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private static void AddEventDetails(List<SecurityScanDetail> details, JsonElement eventElement)
        {
            if (eventElement.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            foreach (var pair in new[] { ("TimeCreated", "Ultimo evento"), ("Id", "Id evento"), ("LevelDisplayName", "Nivel") })
            {
                if (eventElement.TryGetProperty(pair.Item1, out var value))
                {
                    details.Add(Detail(pair.Item2, value.ToString()));
                }
            }
        }

        private static SecurityScanItem Item(string id, string title, string status, string summary, List<SecurityScanDetail> details)
        {
            return new SecurityScanItem
            {
                id = id,
                title = title,
                status = status,
                summary = summary,
                details = details
            };
        }

        private static SecurityScanDetail Detail(string label, string value)
        {
            return new SecurityScanDetail { label = label, value = string.IsNullOrWhiteSpace(value) ? "No detectado" : value };
        }

        private static string ResolveOverallStatus(IEnumerable<SecurityScanItem> items)
        {
            if (items.Any(i => i.status == "danger"))
            {
                return "danger";
            }
            if (items.Any(i => i.status == "warning"))
            {
                return "warning";
            }
            if (items.Any(i => i.status == "unknown"))
            {
                return "unknown";
            }
            return "ok";
        }

        private static string DecodeSecurityCenterProductState(string rawState)
        {
            if (!int.TryParse(rawState, out var state))
            {
                return string.IsNullOrWhiteSpace(rawState) ? "No detectado" : rawState;
            }

            var enabled = (state & 0x1000) != 0 || (state & 0x10) != 0;
            var upToDate = (state & 0x00F0) == 0;
            return $"{(enabled ? "Activo" : "Inactivo")} / {(upToDate ? "Firmas actualizadas" : "Revisar firmas")} ({state})";
        }

        private static string DecodeBitLockerProtection(string value)
        {
            return value switch
            {
                "0" => "Proteccion desactivada",
                "1" => "Proteccion activada",
                "2" => "Proteccion desconocida",
                _ => "No detectado"
            };
        }

        private static string DecodeBitLockerConversion(string value)
        {
            return value switch
            {
                "0" => "Totalmente descifrado",
                "1" => "Totalmente cifrado",
                "2" => "Cifrado en progreso",
                "3" => "Descifrado en progreso",
                "4" => "Cifrado pausado",
                "5" => "Descifrado pausado",
                _ => "No detectado"
            };
        }

        private static string BoolText(string value)
        {
            return value.Equals("True", StringComparison.OrdinalIgnoreCase) || value == "1" ? "Si" : "No";
        }
    }
}
