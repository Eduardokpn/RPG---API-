using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RpgApi.Data;
using RpgApi.Models;
using System.Text;


namespace RpgApi.Controllers
{
    [Route("[controller]")]
    [ApiController]

    public class DisputasController : ControllerBase
    {
        private readonly DataContext _context;
        public DisputasController(DataContext context)
        {
            _context = context;
        } 

        [HttpPost("Arma")]

        public async Task<IActionResult> AtaqueComArmaAsysc(Disputa x)
        {

            try
            {
                Personagem? atacante  = await _context.TB_PERSONAGENS.Include(p => p.Arma).FirstOrDefaultAsync(p => p.Id == x.Id);

                Personagem? oponente = await _context.TB_PERSONAGENS.FirstOrDefaultAsync(p => p.Id == x.OponenteId);

                int dano = atacante.Arma.Dano + (new Random().Next(atacante.Forca));

                dano = dano - new Random().Next(oponente.Defesa);

                if (dano > 0)
                 oponente.PontosVida = oponente.PontosVida - (int)dano;
                if(oponente.PontosVida < 0)
                  x.Narracao = $"{oponente.Nome} foi derrotado !";
                
                _context.TB_PERSONAGENS.Update(oponente);
                await _context.SaveChangesAsync();

                StringBuilder dados = new StringBuilder();

                dados.AppendFormat(" Atacante: {0}.", atacante.Nome);
                dados.AppendFormat("Oponente {0}", oponente.Nome);
                dados.AppendFormat(" Pontos de vida  de atacante: {0}.", atacante.PontosVida);
                dados.AppendFormat(" Pontos de do Oponente: {0}", oponente.Nome);
                dados.AppendFormat(" Arma Utilizado: {0}", atacante.Arma.Nome);
                dados.AppendFormat(" Dano {0}",dano);

                x.Narracao += dano.ToString();
                x.DataDisputa = DateTime.Now;
                _context.TB_DISPUTA.Add(x);
                _context.SaveChanges();
                
                
                return Ok(x);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }

        [HttpPost("Habilidade")]

        public async Task<IActionResult> AtaqueComHabilidadeAsync(Disputa x)

        {
            try
            {
                
                Personagem atacante = await _context.TB_PERSONAGENS.Include(p => p.PersonagemHabilidades).ThenInclude(ph => ph.Habilidade).FirstOrDefaultAsync(p => p.Id == x.AtacanteId);

                Personagem oponente = await _context.TB_PERSONAGENS.FirstOrDefaultAsync(p => p.Id == x.AtacanteId);

                PersonagemHabilidade ph = await _context.TB_PERSONAGENS_HABILIDADES.Include(P => P.Habilidade).FirstOrDefaultAsync(pBusca => pBusca.HabilidadeId == x.HabilidadeId && pBusca.PersonagemId == x.AtacanteId);

                
                return Ok(x);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}