using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ASPServer.Controllers {

    [ApiController]
    [Route("api/statsimage")]
    public class StatisticsImageController : ControllerBase {

        [HttpGet]
        [Route("{classid}/{imageid}")]
        public IActionResult GetStatisticsImage(int classid, int imageid) {
            // [FromQuery]
            // Read image file bytes from db, save to stream as png, return
            // https://stackoverflow.com/questions/12467546/is-there-a-recommended-way-to-return-an-image-using-asp-net-web-api

            byte[] bytes = null;
            int imgc = 0;
            lock (Matcher.Instance.db) {
                foreach (var r in Matcher.Instance.db.Results.Include(r => r.resultData)) {
                    if (r.ClassId == classid) {
                        if (imgc == imageid) {
                            bytes = r.resultData.file;
                            break;
                        }

                        ++imgc;
                    }
                }
            }

            if (bytes == null)
                return NotFound();

            System.Drawing.Image image = System.Drawing.Image.FromStream(new MemoryStream(bytes));

            using (MemoryStream ms = new MemoryStream()) {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

                return File(ms.ToArray(), "image/png");

               // HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
               // result.Content = new ByteArrayContent(ms.ToArray());
               // result.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
               //
               // return result;
            }
        }
    }
}
