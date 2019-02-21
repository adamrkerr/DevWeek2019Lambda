using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace DevWeek2019Lambda.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileController : ControllerBase
    {
        private readonly IAmazonS3 _s3Client;

        private readonly string _s3Bucket;

        public FileController(IConfiguration config, IAmazonS3 s3Client)
        {
            this._s3Client = s3Client;
            this._s3Bucket = config[Startup.AppS3BucketKey];
        }

        //IMPORTANT: When deployed, this returns the file as a base64 string.
        //End consumers need to convert the result to a blob
        [HttpGet("sample")]
        public async Task<IActionResult> GetFileSample()
        {
            var fileStream = new FileStream("HelloDevWeek.xlsx", FileMode.Open, FileAccess.Read);
            var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream);

            var file = new FileContentResult(memoryStream.ToArray(), "application/vnd.ms-excel");
            file.FileDownloadName = "HelloDevWeek.xlsx";
            return file;
        }

        //Copy the file from this project to your s3 bucket to test below
        const string s3File = "HelloDevWeek.txt";

        [HttpGet("link")]
        public async Task<IActionResult> GetFileLink()
        {
            //For demo purposes, assume the file is already in the bucket

            var linkRequest = new GetPreSignedUrlRequest()
            {
                BucketName = _s3Bucket,
                Key = s3File,
                Expires = DateTime.UtcNow.AddMinutes(1)
            };

            var response = _s3Client.GetPreSignedURL(linkRequest);

            return Ok(response);
        }



    }
}