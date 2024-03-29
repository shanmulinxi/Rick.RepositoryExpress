﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rick.RepositoryExpress.SysWebApi.Models;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.IService;
using Rick.RepositoryExpress.Common;
using Microsoft.AspNetCore.Authorization;
using Rick.RepositoryExpress.Utils.Wechat;
using Rick.RepositoryExpress.RedisService;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace Rick.RepositoryExpress.SysWebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChannelpricefileController : RickControllerBase
    {
        private readonly ILogger<ChannelpricefileController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IFileService _fileService;
        private readonly string filePath = "../Uploads/Channelpricedemo/";
        private readonly string fileName = "Channelprice.xlsx";

        public ChannelpricefileController(ILogger<ChannelpricefileController> logger, IFileService fileService, IIdGeneratorService idGenerator)
        {
            _logger = logger;
            _fileService = fileService;
            _idGenerator = idGenerator;
            var env = Environment.GetEnvironmentVariables();
            var os = Convert.ToString(env["OS"]);
            var dr = Convert.ToString(env["SystemDrive"]);
            if (os.Contains("Windows"))
            {
                string currentDirectory = Directory.GetCurrentDirectory();
                DirectoryInfo directory = new DirectoryInfo(currentDirectory);
                directory = directory.Parent;
                filePath = directory.GetDirectories().Where(t => t.Name.Contains("Uploads")).First().FullName + "\\Channelpricedemo\\";
            }
            else
            {
                filePath = dr + "/Uploads/Channelpricedemo/";
            }

        }

        /// <summary>
        /// 文件Url
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<FileContentResult> Get()
        {
            string path = filePath + fileName;
            FileInfo fi = new FileInfo(path);
            if (!fi.Exists)
            {
                return null;
            }
            
            using (var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                byte[] buffer = new byte[fi.Length];
                await fileStream.ReadAsync(buffer, 0, Convert.ToInt32(fi.Length));
                FileContentResult fileContentResult = new FileContentResult(buffer, @"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                return fileContentResult;
            }
        }
    }
}
