using Microsoft.AspNetCore.Mvc;
using System;

namespace ASPServer.Controllers {

    [ApiController]
    [Route("api/[controller]")]
    public class MatchResultController : ControllerBase {

        [HttpDelete]
        public string DeleteMatch() {
            lock (Matcher.Instance.db) { 
                Matcher.Instance.db.Results.RemoveRange(Matcher.Instance.db.Results);
                Matcher.Instance.db.SaveChanges();
            }
            return "ok man";
        }

        [HttpPut]
        public ActionResult<MatchResult> PutMatch([FromBody] string base64data) {
            try {
                byte[] file = Convert.FromBase64String(base64data);
                Tuple<int, int> res = Matcher.Instance.Match(file);
                return new MatchResult { ClassId = res.Item1, Statistics = res.Item2 };
                //return new MatchResult { ClassId = 13, Statistics = 13 };
            } catch {
                return new StatusCodeResult(500);
            }
        }
    }
}
