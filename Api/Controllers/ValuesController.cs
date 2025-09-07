using Entity.Data;
using Entity.Models.Seckill;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        private readonly RoutineDbContext _routineDbContext;
        public ValuesController(RoutineDbContext routineDbContext)
        {
            this._routineDbContext = routineDbContext;
            this._routineDbContext.Database.EnsureCreated();
        }

        [HttpGet("seckillVoucher/{id}")]
        public async Task<IActionResult> GetSeckillVoucher([FromQuery] int id)
        {
            var rct = await this._routineDbContext.SeckillVouchers.FindAsync(id);
            return Ok(rct);
        }

        [HttpPost("seckillVoucher")]
        public async Task<ActionResult<SeckillVoucher>> CreateSeckillVoucher(SeckillVoucher seckill)
        {
            var sv = await this._routineDbContext.SeckillVouchers.AddAsync(seckill);

            await this._routineDbContext.SaveChangesAsync();

            return sv?.Entity;

        }
    }
}
