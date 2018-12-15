﻿using MG.Api;
using System;
using Sonarr.Api.Endpoints;
using Sonarr.Api.Enums;
using Sonarr.Api.Results;
using System.Management.Automation;
using System.Threading;

namespace Sonarr.Api.Cmdlets
{
    public abstract class BaseCommandCmdlet : BaseCmdlet
    {
        internal abstract SonarrCommand Command { get; }
        //public abstract SonarrMethod Method { get; }

        protected private bool _wait;
        [Parameter(Mandatory = false)]
        public SwitchParameter WaitForCompletion
        {
            get => _wait;
            set => _wait = value;
        }

        protected override void BeginProcessing() => base.BeginProcessing();

        //public ApiResult ApplyCommandToAll()
        //{
        //    var rp = new RequestParameters();
        //    var ep = new Command(Command, rp);
        //    if (!_wait)
        //    {
        //        return Api.Send(ep, null, Method, rp);
        //    }
        //    else
        //    {
        //        var temp = Api.Send(ep, null, Method, rp);
        //        return WaitUntilComplete((int)temp[0]["id"]);
        //    }
        //}
        //public ApiResult ApplyCommandToOne(params object[] bodyParameters)
        //{
        //    var rp = new RequestParameters()
        //    {
        //        { bodyParameters }
        //    };
        //    var cmd = new Command(Command, rp);
        //    if (!_wait)
        //    {
        //        return Api.Send(cmd, null, Method, cmd.RequestBody);
        //    }
        //    else
        //    {
        //        var temp = Api.Send(cmd, null, Method, cmd.RequestBody);
        //        return WaitUntilComplete((int)temp[0]["id"]);
        //    }
        //}

        //public ApiResult WaitUntilComplete(int id)
        //{
        //    var cmd = new Command(id);
        //    result = Api.Send(cmd);
        //    while (!result[0]["status"].Equals("completed"))
        //    {
        //        Thread.Sleep(2000);
        //        result = Api.Send(cmd);
        //    }
        //    return result;
        //}

        //public void PipeBackResult(ApiResult result)
        //{
        //    if (!_wait)
        //    {
        //        PipeBack<CommandResult>(result);
        //    }
        //    else
        //    {
        //        PipeBack<FinalCommandResult>(result);
        //    }
        //}
    }
}
