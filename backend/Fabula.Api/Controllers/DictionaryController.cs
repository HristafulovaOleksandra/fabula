using Fabula.Api.Auth;
using Fabula.Api.Contracts;
using Fabula.Api.Dictionary;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fabula.Api.Controllers;

[ApiController]
[Authorize(Policy = Policies.CanAuthor)]
[Route("api/dictionary")]
public class DictionaryController(WordDictionary dictionary) : ControllerBase
{
    [HttpGet]
    public ActionResult<TranslationDto> Translate([FromQuery] string word)
    {
        if (string.IsNullOrWhiteSpace(word) || word.Length > 64)
        {
            return ValidationProblem("Вкажи слово до 64 символів.");
        }

        return Ok(new TranslationDto(word.Trim(), dictionary.Translate(word)));
    }
}
