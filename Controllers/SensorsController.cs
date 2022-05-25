using LoneWorkingBackend.Models;
using LoneWorkingBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace LoneWorkingBackend.Controllers
{
    [ApiController]
    [Route("api/")]
    public class SensorsController
    {
        private readonly SensorService _sensorService;

        public SensorsController(SensorService sensorService)
        {
            _sensorService = sensorService;
        }

        [HttpGet("sensor-data")]
        public async Task<ActionResult<List<Sensor>>> sensorData()
        {
            return await _sensorService.GetAsync();
        }
    }
}