using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ASPServer.Controllers {

    [ApiController]
    [Route("[controller]")]
    public class WebappController : ControllerBase {

        // TODO: Не обрабатывает запрос

        [HttpGet]
        public ActionResult GetWebApp() {
            try {
                return base.Content(System.IO.File.ReadAllText("WEB/index.html"), "text/html");
            } catch (Exception e) {
                return base.Content("Егор", "text/html");
            }
        }
    }
}
