using Core.Data;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sotoped.Models;
using System.IO.Compression;

namespace Sotoped.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StopedController : ControllerBase
    {
        private SotopedContext dbContext { get; set; }
        private readonly ILogger<StopedController> _logger;

        public StopedController(SotopedContext context, ILogger<StopedController> logger)
        {
            dbContext = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            try
            {
                var spectators = await dbContext.Spectators.ToListAsync();
                return Ok(spectators);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An Error Occured in StopedController_GetAll : {message}", ex.Message);
                return BadRequest();
            }
        }


        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            try
            {
                var dbSpectator = await dbContext.Spectators.FindAsync(id);
                if (dbSpectator == null) return NotFound(new { OperationSucess = false, ErrorMessage = "Participant introuvable", code = 404 });
                return Ok(dbSpectator);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An Error Occured in StopedController_GetById : {message}", ex.Message);
                return BadRequest();
            }
        }


        [HttpPost]
        public async Task<ActionResult> Create([FromBody] Spectator spectator)
        {
            try
            {
                if (string.IsNullOrEmpty(spectator.FullName))
                {
                    return BadRequest(new { OperationSucess = false, ErrorMessage = "Le nom du spectateur est obligatoire", code = 400 });
                }

                if (string.IsNullOrEmpty(spectator.Email))
                {
                    return BadRequest(new { OperationSucess = false, ErrorMessage = "Le mail du spectateur est obligatoire", code = 400 });
                }
                spectator.Code = await GetCodeAsync();
                await dbContext.Spectators.AddAsync(spectator);
                await dbContext.SaveChangesAsync();

                return Ok(spectator);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An Error Occured in StopedController_Edit : {message}", ex.Message);
                return BadRequest(new { OperationSucess = false, ErrorMessage = ex.Message, code = 400 });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Edit([FromRoute] int id, [FromBody] Spectator spectator)
        {
            try
            {
                var dbSpectator = await dbContext.Spectators.FindAsync(id);
                if (dbSpectator == null) return NotFound(new { OperationSucess = false, ErrorMessage = "Participant introuvable", code = 404 });

                dbSpectator.Email = spectator.Email;
                dbSpectator.FullName = spectator.FullName;
                dbSpectator.FirstCommunication = spectator.FirstCommunication;
                dbSpectator.ThirdCommunication = spectator.ThirdCommunication;
                dbSpectator.SecondCommunication = spectator.SecondCommunication;
                dbSpectator.PainParticipation = spectator.PainParticipation;
                dbSpectator.CongressParticipation = spectator.CongressParticipation;
                dbSpectator.NephrologicParticipation = spectator.NephrologicParticipation;
                await dbContext.SaveChangesAsync();

                return Ok(dbSpectator);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An Error Occured in StopedController_Edit : {message}", ex.Message);
                return BadRequest(new { OperationSucess = false, ErrorMessage = ex.Message, code = 400 });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                var dbSpectator = await dbContext.Spectators.FindAsync(id);
                if (dbSpectator == null) return NotFound(new { OperationSucess = false, ErrorMessage = "Participant introuvable", code = 404 });

                dbContext.Spectators.Remove(dbSpectator);
                await dbContext.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An Error Occured in StopedController_GetById : {message}", ex.Message);
                return BadRequest();
            }
        }


        [HttpGet("code/{code}")]
        public async Task<ActionResult> GetAttestationListByCode(string code)
        {
            try
            {
                var dbSpectator = (await dbContext.Spectators.Where(s => s.Code != null
                                                    && s.Code.Trim().ToLower() == code.Trim().ToLower()).ToListAsync()).FirstOrDefault();
                if (dbSpectator == null) return NotFound(new { OperationSucess = false, ErrorMessage = "Aucun participant n'est associé à ce code", code = 404 });

                CertificateRequest request = new CertificateRequest
                {
                    Code = code,
                    NephrologicParticipation = dbSpectator.NephrologicParticipation,
                    PainParticipation = dbSpectator.PainParticipation,
                    CongressParticipation = dbSpectator.CongressParticipation,
                    FirstCommunication = !string.IsNullOrWhiteSpace(dbSpectator.FirstCommunication),
                    SecondCommunication = !string.IsNullOrWhiteSpace(dbSpectator.SecondCommunication),
                    ThirdCommunication = !string.IsNullOrWhiteSpace(dbSpectator.ThirdCommunication),
                };
                return Ok(request);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An Error Occured in StopedController_GetAttestationListByCode : {message}", ex.Message);
                return BadRequest(new { OperationSucess = false, ErrorMessage = ex.Message, code = 400 });
            }
        }

        [HttpPost("GenerateCertificate/{code}")]
        public async Task<ActionResult> GenerateCertificate([FromRoute] string code)
        {
            try
            {
                List<(byte[] FileContents, string FileName)> pdfFiles = new();
                var allSpectaors = await dbContext.Spectators.ToListAsync();
                var dbSpectator = (await dbContext.Spectators.Where(s => s.Code != null
                                                    && s.Code == code).ToListAsync()).FirstOrDefault();
                if (dbSpectator == null) return NotFound(new { OperationSucess = false, ErrorMessage = "Participant introuvable", code = 404 });

                if (dbSpectator.PainParticipation)
                {
                    byte[] fileBytes = await GenerateCertificateAsync("./Certificate/Pain.docx", false, null, dbSpectator.FullName);
                    pdfFiles.Add((fileBytes, $"AT_PC_D_{dbSpectator.FullName}.pdf"));
                }
                if (dbSpectator.NephrologicParticipation)
                {
                    byte[] fileBytes = await GenerateCertificateAsync("./Certificate/Nephrologic.docx", false, null, dbSpectator.FullName);
                    pdfFiles.Add((fileBytes, $"AT_PC_N_{dbSpectator.FullName}.pdf"));
                }
                if (dbSpectator.CongressParticipation)
                {
                    byte[] fileBytes = await GenerateCertificateAsync("./Certificate/Congress.docx", false, null, dbSpectator.FullName);
                    pdfFiles.Add((fileBytes, $"AT_Congres_{dbSpectator.FullName}.pdf"));
                }
                if (!string.IsNullOrWhiteSpace(dbSpectator.FirstCommunication))
                {
                    var suffix = !string.IsNullOrWhiteSpace(dbSpectator.SecondCommunication) ? "1_" : string.Empty;
                    byte[] fileBytes = await GenerateCertificateAsync("./Certificate/Communication.docx", true, dbSpectator.FirstCommunication, dbSpectator.FullName);
                    pdfFiles.Add((fileBytes, $"AT_Com_{suffix}{dbSpectator.FullName}.pdf"));
                }
                if (!string.IsNullOrWhiteSpace(dbSpectator.SecondCommunication))
                {
                    byte[] fileBytes = await GenerateCertificateAsync("./Certificate/Communication.docx", true, dbSpectator.SecondCommunication, dbSpectator.FullName);
                    pdfFiles.Add((fileBytes, $"AT_Com_2_{dbSpectator.FullName}.pdf"));
                }
                if (!string.IsNullOrWhiteSpace(dbSpectator.ThirdCommunication))
                {
                    byte[] fileBytes = await GenerateCertificateAsync("./Certificate/Communication.docx", true, dbSpectator.ThirdCommunication, dbSpectator.FullName);
                    pdfFiles.Add((fileBytes, $"AT_Com_3_{dbSpectator.FullName}.pdf"));
                }
                if (pdfFiles.Count == 0) return BadRequest("Aucun certificat à générer.");

                using var memoryStream = new MemoryStream();
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    foreach (var (fileContents, fileName) in pdfFiles)
                    {
                        if (fileContents != null)
                        {
                            var zipEntry = archive.CreateEntry(fileName, CompressionLevel.Optimal);
                            using var entryStream = zipEntry.Open();
                            await entryStream.WriteAsync(fileContents, 0, fileContents.Length);
                        }
                    }
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                var zipFileName = $"Certificats_{dbSpectator.FullName}.zip";

                return File(memoryStream.ToArray(), "application/zip", zipFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An Error Occured in StopedController_GetAttestationListByCode : {message}", ex.Message);
                return BadRequest();
            }
        }


        [HttpPost("GenerateCertificateByCiteria")]
        public async Task<ActionResult> GenerateCertificateByCriteria([FromBody] CertificateRequest certificateRequest)
        {
            try
            {
                FileResponse pdfFiles = new();
                var allSpectaors = await dbContext.Spectators.ToListAsync();
                var dbSpectator = (await dbContext.Spectators.Where(s => s.Code != null
                                                    && s.Code == certificateRequest.Code).ToListAsync()).FirstOrDefault();
                if (dbSpectator == null) return NotFound(new { OperationSucess = false, ErrorMessage = "Aucun participant n'est associé à ce code", code = 404 });

                if (certificateRequest.PainParticipation && dbSpectator.PainParticipation)
                {
                    byte[] fileBytes = await GenerateCertificateAsync("./Certificate/Pain.docx", false, null, dbSpectator.FullName);
                    pdfFiles = new FileResponse(true, fileBytes, $"AT_PC_D_{dbSpectator.FullName}.pdf");
                    return Ok(pdfFiles);
                }
                if (certificateRequest.NephrologicParticipation && dbSpectator.NephrologicParticipation)
                {
                    byte[] fileBytes = await GenerateCertificateAsync("./Certificate/Nephrologic.docx", false, null, dbSpectator.FullName);
                    pdfFiles = new FileResponse(true, fileBytes, $"AT_PC_N_{dbSpectator.FullName}.pdf");
                    return Ok(pdfFiles);
                }
                if (certificateRequest.CongressParticipation && dbSpectator.CongressParticipation)
                {
                    byte[] fileBytes = await GenerateCertificateAsync("./Certificate/Congress.docx", false, null, dbSpectator.FullName);
                    pdfFiles = new FileResponse(true, fileBytes, $"AT_Congres_{dbSpectator.FullName}.pdf");
                    return Ok(pdfFiles);
                }
                if (certificateRequest.FirstCommunication && !string.IsNullOrWhiteSpace(dbSpectator.FirstCommunication))
                {
                    var suffix = !string.IsNullOrWhiteSpace(dbSpectator.SecondCommunication) ? "1_" : string.Empty;
                    byte[] fileBytes = await GenerateCertificateAsync("./Certificate/Communication.docx", true, dbSpectator.FirstCommunication, dbSpectator.FullName);
                    pdfFiles = new FileResponse(true, fileBytes, $"AT_Com_{suffix}{dbSpectator.FullName}.pdf");
                    return Ok(pdfFiles);
                }
                if (certificateRequest.SecondCommunication && !string.IsNullOrWhiteSpace(dbSpectator.SecondCommunication))
                {
                    byte[] fileBytes = await GenerateCertificateAsync("./Certificate/Communication.docx", true, dbSpectator.SecondCommunication, dbSpectator.FullName);
                    pdfFiles = new FileResponse(true, fileBytes, $"AT_Com_2_{dbSpectator.FullName}.pdf");
                    return Ok(pdfFiles);
                }
                if (certificateRequest.ThirdCommunication && !string.IsNullOrWhiteSpace(dbSpectator.ThirdCommunication))
                {
                    byte[] fileBytes = await GenerateCertificateAsync("./Certificate/Communication.docx", true, dbSpectator.ThirdCommunication, dbSpectator.FullName);
                    pdfFiles = new FileResponse(true, fileBytes, $"AT_Com_3_{dbSpectator.FullName}.pdf");
                    return Ok(pdfFiles);
                }
                return BadRequest(new { OperationSucess = false, ErrorMessage = "Une erreur s'est produite.", code = 400 });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An Error Occured in StopedController_GetAttestationListByCode : {message}", ex.Message);
                return BadRequest(new { OperationSucess = false, ErrorMessage = ex.Message, code = 400 });
            }
        }

        private async Task<byte[]> GenerateCertificateAsync(string path, bool hasCommunication, string communication, string spectatorFullName)
        {
            try
            {
                if (!System.IO.File.Exists(path))
                {
                    throw new FileNotFoundException("Le fichier Word spécifié est introuvable.", path);
                }

                using (MemoryStream resultStream = new MemoryStream())
                {
                    using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
                    {
                        await fileStream.CopyToAsync(resultStream);
                    }

                    using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(resultStream, true))
                    {
                        MainDocumentPart mainPart = wordDoc.MainDocumentPart;
                        if (mainPart == null)
                        {
                            throw new Exception("Impossible de trouver le contenu du document.");
                        }

                        ReplaceBookmarkText(mainPart, "FullName", spectatorFullName);
                        if (hasCommunication)
                        {
                            ReplaceBookmarkText(mainPart, "Communication", communication);
                        }

                        wordDoc.Save();
                    }

                    using (MemoryStream convertedPdfStream = new MemoryStream())
                    {
                        Aspose.Words.License lic = new Aspose.Words.License();
                        lic.SetLicense(@"License/Aspose.Words.NET.lic");

                        var document = new Aspose.Words.Document(resultStream);
                        document.Save(convertedPdfStream, Aspose.Words.SaveFormat.Pdf);

                        return convertedPdfStream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération du certificat : {Message}", ex.Message);
                return null;
            }
        }
        private void ReplaceBookmarkText(MainDocumentPart mainPart, string bookmarkName, string newText)
        {
            BookmarkStart bookmark = mainPart.Document.Body
                .Descendants<BookmarkStart>()
                .FirstOrDefault(b => b.Name == bookmarkName);

            if (bookmark != null)
            {
                Run run = bookmark.NextSibling<Run>();
                if (run != null)
                {
                    run.GetFirstChild<Text>().Text = newText;
                    return;
                }

                bookmark.Parent.Append(new Run(new Text(newText)));
            }
        }


        private async Task<string> GetCodeAsync()
        {
            var spectatorsWithCodeList = await dbContext.Spectators.Where(s => !string.IsNullOrEmpty(s.Code)).ToListAsync();
            var oldCodes = spectatorsWithCodeList?.Select(s => s.Code).ToList();

            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random random = new Random();
            string newCode = string.Empty;

            while (string.IsNullOrEmpty(newCode))
            {
                var tempCode = new string(Enumerable.Range(0, 6)
                    .Select(_ => chars[random.Next(chars.Length)])
                    .ToArray());
     
                if (oldCodes == null || (oldCodes != null && !oldCodes.Contains(tempCode))) newCode = tempCode;
            }

            return newCode;
        }

    }
}
