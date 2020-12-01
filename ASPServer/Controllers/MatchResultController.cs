﻿using Microsoft.AspNetCore.Mvc;
using System;

namespace ASPServer.Controllers {

    [ApiController]
    [Route("api/[controller]")]
    public class MatchResultController : ControllerBase {

        [HttpGet]
        public string GetMatch() {
            lock (Matcher.Instance.db) { 
                Matcher.Instance.db.Results.RemoveRange(Matcher.Instance.db.Results);
                Matcher.Instance.db.SaveChanges();
            }
            return "ok man";
        }

        [HttpPut]
        public ActionResult<MatchResult> GetMatch([FromBody] string base64data) {
            Console.WriteLine("Match : " + base64data);
            try {
                byte[] file = Convert.FromBase64String(base64data);
                // return Matcher.Instance.Match(file);
                return new MatchResult { ClassId = 13, Statistics = 13 };
            } catch {
                return new StatusCodeResult(500);
            }
        }
    }
}