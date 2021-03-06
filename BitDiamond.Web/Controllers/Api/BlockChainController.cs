﻿using Axis.Luna;
using Axis.Luna.Extensions;
using BitDiamond.Core.Models;
using BitDiamond.Core.Services;
using BitDiamond.Core.Utils;
using BitDiamond.Web.Controllers.Api.BlockChainModels;
using BitDiamond.Web.Infrastructure.Exceptions;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Web.Http;
using static Axis.Luna.Extensions.ExceptionExtensions;
using BitDiamond.Web.Infrastructure.Utils;
using System.IO;
using System.Web.Hosting;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace BitDiamond.Web.Controllers.Api
{
    public class BlockChainController : ApiController
    {
        private IBlockChainService _blockChain = null;

        public BlockChainController(IBlockChainService blockChain)
        {
            ThrowNullArguments(() => blockChain);

            this._blockChain = blockChain;
        }

        [HttpGet, Route("api/block-chain/btc-xe")]
        public HttpResponseMessage GetExchangeRate()
        {
            using (var client = new WebClient())
            {
                var v = client.DownloadString("https://blockchain.info/tobtc?currency=USD&value=1");

                var response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new StringContent(v);
                response.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
                return response;
            }
        }

        [HttpGet, Route("api/block-chain/transactions/all")]
        public IHttpActionResult GetAllUserTransactions()
        => this.Log(() => _blockChain.GetAllUserTransactions().OperationResult(Request));

        [HttpGet, Route("api/block-chain/transactions/incoming")]
        public IHttpActionResult GetIncomingUserTransactions(string data)
        => this.Log(() => Operation.Try(() => ThrowIfFail(() => Encoding.UTF8.GetString(Convert.FromBase64String(data)), ex => new MalformedApiArgumentsException()))
            .Then(_jopr => ThrowIfFail(() => JsonConvert.DeserializeObject<PagedQueryArgs>(_jopr.Result, Constants.Misc_DefaultJsonSerializerSettings), ex => new MalformedApiArgumentsException()))
            .Then(argopr => _blockChain.GetIncomingUserTransactions(argopr.Result.PageSize, argopr.Result.PageIndex))
            .OperationResult(Request));

        [HttpGet, Route("api/block-chain/transactions/outgoing")]
        public IHttpActionResult GetOutgoingUserTransactions(string data)
        => this.Log(() => Operation.Try(() => ThrowIfFail(() => Encoding.UTF8.GetString(Convert.FromBase64String(data)), ex => new MalformedApiArgumentsException()))
            .Then(_jopr => ThrowIfFail(() => JsonConvert.DeserializeObject<PagedQueryArgs>(_jopr.Result, Constants.Misc_DefaultJsonSerializerSettings), ex => new MalformedApiArgumentsException()))
            .Then(argopr => _blockChain.GetOutgoingUserTransactions(argopr.Result.PageSize, argopr.Result.PageIndex))
            .OperationResult(Request));


        [HttpGet, Route("api/block-chain/transactions/incoming/total")]
        public IHttpActionResult GetIncomingUserTransactionsTotal()
        => this.Log(() => _blockChain.GetIncomingUserTransactionsTotal().OperationResult(Request));

        [HttpGet, Route("api/block-chain/transactions/outgoing/total")]
        public IHttpActionResult GetOutgoingUserTransactionsTotal()
        => this.Log(() => _blockChain.GetOutgoingUserTransactionsTotal().OperationResult(Request));


        [HttpGet, Route("api/block-chain/transactions/system/total")]
        public IHttpActionResult GetSystemTransactionsTotal()
        => this.Log(() => _blockChain.GetSystemTransactionsTotal()
            .Then(opr => //remove this later
            {
                var x = opr.Result;
                new StreamReader(new FileStream(HostingEnvironment.MapPath("~/App_Data/pad.json"), FileMode.OpenOrCreate)).Using(_r =>
                {
                    var json = _r.ReadToEnd();
                    var pad = JsonConvert.DeserializeObject<Pad>(json);
                    x += pad.btc;
                });

                return x;
            })
            .OperationResult(Request));


        [HttpPut, Route("api/block-chain/transactions/verify-manually")]
        public IHttpActionResult VerifyManually([FromBody] TransactionVerificationArgs args)
        => this.Log(() =>
        {
            Operation<BlockChainTransaction> opr;
            if (args?.TransactionId > 0) opr = _blockChain.VerifyManually(args.TransactionId ?? 0);
            else if (!string.IsNullOrWhiteSpace(args?.TransactionHash)) opr = _blockChain.VerifyManually(args.TransactionHash);
            else opr = Operation.Fail<BlockChainTransaction>("Invalid arguments");

            return opr.OperationResult(Request);
        });
    }

    namespace BlockChainModels
    {
        public class PagedQueryArgs
        {
            public int PageSize { get; set; }
            public int PageIndex { get; set; }
        }

        public class TransactionVerificationArgs
        {
            public long? TransactionId { get; set; }
            public string TransactionHash { get; set; }
        }
    }
}
