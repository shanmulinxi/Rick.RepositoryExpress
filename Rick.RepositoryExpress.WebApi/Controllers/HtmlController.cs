﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Rick.RepositoryExpress.WebApi.Models;
using Rick.RepositoryExpress.DataBase.Models;
using Rick.RepositoryExpress.IService;
using Rick.RepositoryExpress.Common;
using Microsoft.AspNetCore.Authorization;
using Rick.RepositoryExpress.Utils.Wechat;
using Rick.RepositoryExpress.RedisService;
using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace Rick.RepositoryExpress.WebApi.Controllers
{
    [Route("api/[controller]/{id?}")]
    [ApiController]
    public class HtmlController : RickControllerBase
    {
        private readonly ILogger<HtmlController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IFileService _fileService;
        private readonly string filePath = "F:\\Uploads\\";

        public HtmlController(ILogger<HtmlController> logger, IFileService fileService, IIdGeneratorService idGenerator)
        {
            _logger = logger;
            _fileService = fileService;
            _idGenerator = idGenerator;
        }

        /// <summary>
        /// 文件Url
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public async Task<FileContentResult> Get(long id)
        {
            Fileinfo fileinfo = await _fileService.FindAsync<Fileinfo>(id);
            string path = filePath + fileinfo.Filename + fileinfo.Ext;
            FileInfo fi = new FileInfo(path);
            if (!fi.Exists)
            {
                return null;
            }
            using (var fileStream = new FileStream(filePath + fileinfo.Filename + fileinfo.Ext, FileMode.Open))
            {
                byte[] buffer = new byte[fi.Length];
                await fileStream.ReadAsync(buffer, 0, Convert.ToInt32(fi.Length));
                FileContentResult fileContentResult = new FileContentResult(buffer, fileinfo.Mime);
                return fileContentResult;
            }
        }
    }
}