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
using Rick.RepositoryExpress.Utils;
using Microsoft.EntityFrameworkCore;

namespace Rick.RepositoryExpress.WebApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrderErrorController : RickControllerBase
    {
        private readonly ILogger<OrderErrorController> _logger;
        private readonly IIdGeneratorService _idGenerator;
        private readonly IPackageOrderApplyService _packageOrderApplyService;
        private readonly RedisClientService _redisClientService;

        public OrderErrorController(ILogger<OrderErrorController> logger, IPackageOrderApplyService packageOrderApplyService, IIdGeneratorService idGenerator, RedisClientService redisClientService)
        {
            _logger = logger;
            _packageOrderApplyService = packageOrderApplyService;
            _idGenerator = idGenerator;
            _redisClientService = redisClientService;
        }

        /// <summary>
        /// 查询问题件
        /// </summary>
        /// <param name="index"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<RickWebResult<OrderErrorResponseList>> Get([FromQuery] int index = 1, [FromQuery] int pageSize = 10)
        {
            OrderErrorResponseList orderErrorResponseList = new OrderErrorResponseList();
            List<Packageorderapplyerror> packageorderapplyerrors = await _packageOrderApplyService.Query<Packageorderapplyerror>(error => error.Appuser == UserInfo.Id).ToListAsync();
            List<long> orderIds = packageorderapplyerrors.Select(t => t.Packageorderapplyid).ToList();

            var query = from order in _packageOrderApplyService.Query<Packageorderapply>(t => orderIds.Contains(t.Id))
                        select order;

            orderErrorResponseList.Count = await query.CountAsync();
            var results = await query.OrderByDescending(t => t.Id).Skip((index - 1) * pageSize).Take(pageSize).ToListAsync();
            orderErrorResponseList.List = new List<OrderErrorResponse>();
            foreach (Packageorderapply packageorderapply in results)
            {

                Packageorderapplyerror packageorderapplyerror = await _packageOrderApplyService.Query<Packageorderapplyerror>(error => error.Packageorderapplyid == packageorderapply.Id).OrderByDescending(t => t.Id).FirstAsync();
                OrderErrorResponse orderErrorResponse = new OrderErrorResponse();
                orderErrorResponse.Code = packageorderapply.Code;
                orderErrorResponse.Id = packageorderapply.Id;
                if (packageorderapplyerror != null)
                {
                    Packageorderapplyerrorlog packageorderapplyerrorlog = new Packageorderapplyerrorlog();
                    var packageorderapplyerrorlogs = await _packageOrderApplyService.Query<Packageorderapplyerrorlog>(log => log.Status == 1 && log.Packageorderapplyerrorid == packageorderapplyerror.Id).OrderByDescending(t => t.Id).ToListAsync();
                    orderErrorResponse.Name = packageorderapplyerror.Name;
                    orderErrorResponse.Remark = packageorderapplyerror.Remark;
                    orderErrorResponse.Endremark = packageorderapplyerror.Endremark;
                    orderErrorResponse.Status = packageorderapplyerror.Status;
                    orderErrorResponse.Lasttime = packageorderapplyerror.Lasttime;

                    orderErrorResponse.List = packageorderapplyerrorlogs.Select(log => new OrderErrorResponseLog()
                    {
                        Id = log.Id,
                        Type = log.Type,
                        Remark = log.Remark,
                        Addtime = log.Addtime
                    }).ToList();
                }
                Packageorderapplyexpress packageorderapplyexpress = await _packageOrderApplyService.FindAsync<Packageorderapplyexpress>(t => t.Packageorderapplyid == packageorderapply.Id && t.Status != 0);
                if (packageorderapplyexpress != null)
                {
                    orderErrorResponse.Outnumber = packageorderapplyexpress.Outnumber;
                    orderErrorResponse.Price = packageorderapplyexpress.Price ?? 0;
                }
                Appuseraddress appuseraddress = await _packageOrderApplyService.FindAsync<Appuseraddress>(packageorderapply.Addressid);
                if (appuseraddress != null)
                {
                    Nation nation = await _packageOrderApplyService.FindAsync<Nation>(appuseraddress.Nationid);
                    orderErrorResponse.Address = string.Format("{0}{1}{2}", nation != null ? nation.Name : string.Empty, appuseraddress.Region, appuseraddress.Address);
                }
                orderErrorResponseList.List.Add(orderErrorResponse);

            }
            return RickWebResult.Success(orderErrorResponseList);
        }

        [HttpPost]
        public async Task<RickWebResult<object>> Post([FromBody] OrderErrorRequest orderApplyRequest)
        {
            await _packageOrderApplyService.BeginTransactionAsync();
            DateTime now = DateTime.Now;

            Packageorderapply packageorderapply = await _packageOrderApplyService.FindAsync<Packageorderapply>(orderApplyRequest.Id);
            Packageorderapplyerror packageorderapplyerror = await _packageOrderApplyService.Query<Packageorderapplyerror>(error => error.Status == 1 && error.Packageorderapplyid == packageorderapply.Id).OrderByDescending(t => t.Id).FirstOrDefaultAsync();

            bool isAppend = packageorderapplyerror != null;

            packageorderapply.Orderstatus = (int)OrderApplyStatus.问题件;
            packageorderapply.Lasttime = now;
            packageorderapply.Lastuser = UserInfo.Id;
            await _packageOrderApplyService.UpdateAsync(packageorderapply);

            if (isAppend)
            {
                packageorderapplyerror.Lasttime = now;
                packageorderapplyerror.Lastuser = UserInfo.Id;

                await _packageOrderApplyService.UpdateAsync(packageorderapplyerror);

                Packageorderapplyerrorlog packageorderapplyerrorlog = new Packageorderapplyerrorlog();
                packageorderapplyerrorlog.Id = _idGenerator.NextId();
                packageorderapplyerrorlog.Packageorderapplyerrorid = packageorderapplyerror.Id;
                packageorderapplyerrorlog.Appuser = packageorderapply.Appuser;
                packageorderapplyerrorlog.Type = 2;
                packageorderapplyerrorlog.Status = 1;
                packageorderapplyerrorlog.Adduser = UserInfo.Id;
                packageorderapplyerrorlog.Addtime = now;
                packageorderapplyerrorlog.Lastuser = UserInfo.Id;
                packageorderapplyerrorlog.Lasttime = now;
                packageorderapplyerrorlog.Remark = orderApplyRequest.Remark;
                await _packageOrderApplyService.AddAsync(packageorderapplyerrorlog);
            }
            else
            {
                packageorderapplyerror = new Packageorderapplyerror();
                packageorderapplyerror.Id = _idGenerator.NextId();
                packageorderapplyerror.Packageorderapplyid = packageorderapply.Id;
                packageorderapplyerror.Appuser = packageorderapply.Appuser;
                packageorderapplyerror.Status = 1;
                packageorderapplyerror.Adduser = UserInfo.Id;
                packageorderapplyerror.Addtime = now;
                packageorderapplyerror.Lastuser = UserInfo.Id;
                packageorderapplyerror.Lasttime = now;
                packageorderapplyerror.Remark = orderApplyRequest.Remark;
                packageorderapplyerror.Name = String.Empty;

                Packageorderapplyerrorlog packageorderapplyerrorlog = new Packageorderapplyerrorlog();
                packageorderapplyerrorlog.Id = _idGenerator.NextId();
                packageorderapplyerrorlog.Packageorderapplyerrorid = packageorderapplyerror.Id;
                packageorderapplyerrorlog.Appuser = packageorderapply.Appuser;
                packageorderapplyerrorlog.Type = 2;
                packageorderapplyerrorlog.Status = 1;
                packageorderapplyerrorlog.Adduser = UserInfo.Id;
                packageorderapplyerrorlog.Addtime = now;
                packageorderapplyerrorlog.Lastuser = UserInfo.Id;
                packageorderapplyerrorlog.Lasttime = now;
                packageorderapplyerrorlog.Remark = orderApplyRequest.Remark;
                await _packageOrderApplyService.AddAsync(packageorderapplyerrorlog);

                var packageorderapplydetailes = await _packageOrderApplyService.QueryAsync<Packageorderapplydetail>(t => t.Packageorderapplyid == packageorderapply.Id);
                foreach (var packageorderapplydetaile in packageorderapplydetailes)
                {
                    var package = await _packageOrderApplyService.FindAsync<Package>(packageorderapplydetaile.Packageid);
                    package.Status = (int)PackageStatus.问题件;
                    package.Lastuser = UserInfo.Id;
                    package.Lasttime = now;
                    await _packageOrderApplyService.UpdateAsync(package);
                    packageorderapplyerror.Name += package.Name;

                    Packagenote packagenote = new Packagenote();
                    packagenote.Id = _idGenerator.NextId();
                    packagenote.Packageid = package.Id;
                    packagenote.Status = 1;
                    packagenote.Adduser = UserInfo.Id;
                    packagenote.Addtime = now;
                    packagenote.Isclosed = 0;
                    packagenote.Operator = (int)PackageNoteStatus.问题件;
                    packagenote.Operatoruser = UserInfo.Id;
                    await _packageOrderApplyService.AddAsync(packagenote);
                }

                Packageorderapplynote packageorderapplynote = new Packageorderapplynote();
                packageorderapplynote.Id = _idGenerator.NextId();
                packageorderapplynote.Packageorderapplyid = packageorderapply.Id;
                packageorderapplynote.Status = 1;
                packageorderapplynote.Adduser = UserInfo.Id;
                packageorderapplynote.Addtime = now;
                packageorderapplynote.Isclosed = 0;
                packageorderapplynote.Operator = (int)OrderApplyStatus.问题件;
                packageorderapplynote.Operatoruser = UserInfo.Id;
                await _packageOrderApplyService.AddAsync(packageorderapplynote);

                await _packageOrderApplyService.AddAsync(packageorderapplyerror);

            }

            await _packageOrderApplyService.CommitAsync();
            return RickWebResult.Success(new object());
        }


        public class OrderErrorResponseList
        {
            public int Count { get; set; }
            public List<OrderErrorResponse> List { get; set; }
        }
        public class OrderErrorResponseLog
        {
            public long Id { get; set; }
            public int Type { get; set; }
            public string Remark { get; set; }
            public DateTime Addtime { get; set; }
        }

        public class OrderErrorResponse
        {
            public long Id { get; set; }
            public string Code { get; set; }
            public string Outnumber { get; set; }
            public string Name { get; set; }
            public string Address { get; set; }
            public string Remark { get; set; }
            public string Endremark { get; set; }

            public DateTime Lasttime { get; set; }

            public int Status { get; set; }
            public decimal Price { get; set; }
            public int Count { get; set; }
            public List<OrderErrorResponseLog> List { get; set; }

        }

        public class OrderErrorRequest
        {
            public long Id { get; set; }
            public string Remark { get; set; }

        }

    }
}
