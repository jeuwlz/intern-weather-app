using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using API.Models;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;

namespace API.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class DataController : ControllerBase
    {

        public readonly IConfiguration _configuration;

        public DataController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet, Authorize(Roles = "Admin")]
        [Route("GetAllData")]
        public string GetData()
        {
            SqlConnection con = new SqlConnection(_configuration.GetConnectionString("DataAppCon").ToString());
            SqlDataAdapter da = new SqlDataAdapter("SELECT * FROM Station_Measurements ORDER BY Measurement_Time ASC", con);
            DataTable dt = new DataTable();
            da.Fill(dt);
            List<Data> dataList = new List<Data>();
            Response response= new Response();
            if (dt.Rows.Count > 0)
            {
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    Data data = new Data();
                    data.Id = Convert.ToInt32(dt.Rows[i]["Station_Measurements_ID"]);
                    data.Measurement_Time = Convert.ToDateTime(dt.Rows[i]["Measurement_Time"]);
                    data.Temperature = (float)Math.Round(Convert.ToSingle(dt.Rows[i]["Temperature"]), 2);
                    data.Humidity = (float)Math.Round(Convert.ToSingle(dt.Rows[i]["Humidity"]), 1);
                    data.Wind_Speed = (float)Math.Round(Convert.ToSingle(dt.Rows[i]["Wind_Speed"]), 1);
                    dataList.Add(data);
                }
            }
            if (dataList.Count > 0)
                {
                    return JsonConvert.SerializeObject(dataList);
                }
                else
                {
                    response.StatusCode = 100;
                    response.ErrorMessage = "No data found";
                    return JsonConvert.SerializeObject(response);
                }
                
            }
        }
    }

