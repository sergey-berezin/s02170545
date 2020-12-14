using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;

namespace ASPServer.Controllers {

    [ApiController]
    [Route("api/[controller]")]
    class StatisticsImageController : ControllerBase {

        [HttpGet]
        [Route("api/[controller]/{classid}/{imageid}")]
        public HttpResponseMessage GetStatisticsImage(int classid, int imageid) {
            // [FromQuery]
            // Read image file bytes from db, save to stream as png, return
            // https://stackoverflow.com/questions/12467546/is-there-a-recommended-way-to-return-an-image-using-asp-net-web-api

            byte[] bytes = null;
            int imgc = 0;
            lock (Matcher.Instance.db) {
                foreach (Result r in Matcher.Instance.db.Results) {
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
                return new HttpResponseMessage(HttpStatusCode.NotFound);

            System.Drawing.Image image = System.Drawing.Image.FromStream(new MemoryStream(bytes));

            using (MemoryStream ms = new MemoryStream()) {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

                HttpResponseMessage result = new HttpResponseMessage(HttpStatusCode.OK);
                result.Content = new ByteArrayContent(ms.ToArray());
                result.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");

                return result;
            }
        }
    }
}
