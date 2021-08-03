using Microsoft.AspNetCore.Mvc;
namespace WMSWebAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        /// <summary>
        /// Use in test connection of the server
        /// If send in id is 9 will return 9 as sucesss
        /// else it will return value as not verified
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        // GET api/values/5
        public string Get(int id)
        {
            if (id == 9) return "connection tested " + id.ToString();
            else return "value";
        }
    }
}