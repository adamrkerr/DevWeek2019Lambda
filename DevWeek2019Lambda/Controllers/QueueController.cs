using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DevWeek2019Lambda.Models;
using Amazon.SQS;
using Microsoft.Extensions.Configuration;
using Amazon.SQS.Model;

namespace DevWeek2019Lambda.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QueueController : ControllerBase
    {
        private readonly IAmazonSQS _sqsClient;
        private readonly string _queueUrl;

        public QueueController(IConfiguration config, IAmazonSQS sqsClient)
        {
            this._sqsClient = sqsClient;
            this._queueUrl = config[Startup.AppQueueUrl];
        }

        [HttpPost]
        public async Task<IActionResult> PostMessage([FromBody] QueueRequest message)
        {
            var request = new SendMessageRequest
            {
                MessageBody = message.Message,
                QueueUrl = _queueUrl
            };

            await _sqsClient.SendMessageAsync(request);

            return Ok($"Message '{message.Message}' from user {message.User} queued.");
        }
    }
}