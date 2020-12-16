using Microsoft.AspNetCore.Mvc;
using System;

namespace ASPServer.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class StatisticsController : ControllerBase {

        [HttpGet]
        public int[] GetStatistics() {
            int[] classInfo = new int[Labels.classLabels.Length];

            lock (Matcher.Instance.db) {
                foreach (Result r in Matcher.Instance.db.Results) {
                    ++classInfo[r.ClassId];
                }
            }

            return classInfo;
        }
    }
}
