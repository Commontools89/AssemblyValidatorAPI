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
            var results = new List<ValidationResult>();

            if (!Directory.Exists(request.Path))
            {
                return BadRequest(new List<ValidationResult>
                {
                    new ValidationResult
                    {
                        Status = ValidationStatus.Error,
                        Message = "Provided path does not exist."
                    }
                });
            }

            var configData = new Dictionary<string, string>();
            var configFiles = Directory.GetFiles(request.Path, "*.config");

            if (configFiles.Length == 0)
            {
                return BadRequest(new List<ValidationResult>
                {
                    new ValidationResult
                    {
                        Status = ValidationStatus.Error,
                        Message = "No config files found."
                    }
                });
            }

            foreach (var configPath in configFiles)
            {
                try
                {
                    var xdoc = XDocument.Load(configPath);
                    foreach (var element in xdoc.Root.Elements("assembly"))
                    {
                        var name = element.Attribute("name")?.Value;
                        var version = element.Attribute("version")?.Value;
                        if (name != null && version != null)
                        {
                            configData[name] = version;
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

                var fileVersion = FileVersionInfo.GetVersionInfo(assemblyPath).FileVersion;
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
