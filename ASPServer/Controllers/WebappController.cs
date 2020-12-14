using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ASPServer.Controllers {

    [ApiController]
    [Route("api/weball")]
    class WebappController : ControllerBase {

        // TODO: Не обрабатывает запрос

        [HttpGet]
        public ActionResult GetWebApp() {
            Console.WriteLine("AAAAA0");
            Trace.WriteLine("AAAAA0");
            return base.Content("AAAAAAAAAAAAAAAAA", "text/html");
            try {
                return base.Content(System.IO.File.ReadAllText("WEB/index.html"), "text/html");
            } catch (Exception e) {
                return base.Content("Егор", "text/html");
            }
        }
    }
}
