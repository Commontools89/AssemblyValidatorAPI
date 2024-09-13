using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using AssemblyValidatorAPI.Models; // Ensure to import the namespace where ValidationRequest is located
using System.Reflection; // Added for FileVersionInfo

namespace AssemblyValidatorAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ValidationController : ControllerBase
    {
        [HttpPost]
        public ActionResult<List<ValidationResult>> ValidateAssemblies([FromBody] ValidationRequest request)
        {
            if (string.IsNullOrEmpty(request.Path))
            {
                return BadRequest(new ValidationResult
                {
                    Status = ValidationStatus.Error,
                    Message = "Provided path is null or empty."
                });
            }

            var results = new List<ValidationResult>();

            if (!Directory.Exists(request.Path))
            {
                return BadRequest(new ValidationResult
                {
                    Status = ValidationStatus.Error,
                    Message = "Provided path does not exist."
                });
            }

            var configData = new Dictionary<string, string>();
            var configFiles = Directory.GetFiles(request.Path, "*.config");

            if (configFiles.Length == 0)
            {
                return BadRequest(new ValidationResult
                {
                    Status = ValidationStatus.Error,
                    Message = "No config files found."
                });
            }

            foreach (var configPath in configFiles)
            {
                try
                {
                    var xdoc = XDocument.Load(configPath);
                    var root = xdoc.Root;
                    if (root != null)
                    {
                        foreach (var element in root.Elements("assembly"))
                        {
                            var name = element.Attribute("name")?.Value;
                            var version = element.Attribute("version")?.Value;
                            if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(version))
                            {
                                configData[name] = version;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    results.Add(new ValidationResult
                    {
                        Status = ValidationStatus.Error,
                        Message = $"Error parsing config file {Path.GetFileName(configPath)}: {ex.Message}"
                    });
                }
            }

            foreach (var assembly in configData)
            {
                var assemblyPath = Path.Combine(request.Path, assembly.Key);
                if (!System.IO.File.Exists(assemblyPath))
                {
                    results.Add(new ValidationResult
                    {
                        AssemblyName = assembly.Key,
                        ExpectedVersion = assembly.Value,
                        Status = ValidationStatus.Error,
                        Message = "Assembly file not found."
                    });
                    continue;
                }

                var fileVersionInfo = FileVersionInfo.GetVersionInfo(assemblyPath);
                var fileVersion = fileVersionInfo.FileVersion ?? "Unknown";

                if (fileVersion != assembly.Value)
                {
                    results.Add(new ValidationResult
                    {
                        AssemblyName = assembly.Key,
                        ExpectedVersion = assembly.Value,
                        ActualVersion = fileVersion,
                        Status = ValidationStatus.Mismatch,
                        Message = "Version mismatch."
                    });
                }
                else
                {
                    results.Add(new ValidationResult
                    {
                        AssemblyName = assembly.Key,
                        ExpectedVersion = assembly.Value,
                        ActualVersion = fileVersion,
                        Status = ValidationStatus.Match,
                        Message = "Version match."
                    });
                }
            }

            return Ok(results);
        }
    }
}
