﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using VkNet.Abstractions;
using VkNet.Model;
using VkNet.Utils;
using vkBotCore;

namespace vkBotCore.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CallbackController : ControllerBase
    {
        public BotCore Core { get; set; }

        private readonly IConfiguration _configuration;

        private readonly IVkApi _vkApi;

        private readonly string _secretKey;

        public CallbackController(BotCore core)
        {
            try
            {
                Core = core;
                _configuration = Core.Configuration;
                _vkApi = Core.VkApi;
                _secretKey = _configuration.GetValue<string>("Config:SecretKey", null);
            }
            catch (Exception e)
            {
                Core.Log.Error(e.ToString());
            }
        }

        public IActionResult Callback([FromBody]Updates updates)
        {
            try
            {
                if (updates.SecretKey != _secretKey)
                    return BadRequest("Secret key is incorrect!");

                Core.PluginManager.PluginCallbackHandler(ref updates, updates.GroupId);
                if (updates == null) return Ok("ok");

                switch (updates.Type)
                {
                    case "confirmation":
                        return Ok(_configuration.GetValue($"Config:Groups:{updates.GroupId}:Confirmation", _configuration["Config:Confirmation"]));
                    case "message_new":
                        {
                            var vkApi = Core.VkApi.Get(updates.GroupId);
                            var msg = Message.FromJson(new VkResponse(updates.Object));

                            User user = null;
                            try { user = new User(vkApi, msg.FromId.Value); } catch { return Ok("ok"); }

                            vkApi.MessageHandler.OnMessage(user, msg.Text, msg.PeerId.Value, msg, updates.GroupId);
                            break;
                        }
                }
            }
            catch (Exception e)
            {
                Core.Log.Error(e.ToString());
            }

            return Ok("ok");
        }
    }
}