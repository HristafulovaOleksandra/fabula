using Fabula.Api.Economy;
using Microsoft.AspNetCore.Mvc;

namespace Fabula.Api.Controllers;

[ApiController]
[Route("api/economy")]
public class EconomyController(EconomySettings economy) : ControllerBase
{
    [HttpGet]
    public ActionResult<EconomySettings> Get() => Ok(economy);
}
