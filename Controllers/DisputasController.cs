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
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Collections.Generic;


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
                Personagem? atacante = await _context.TB_PERSONAGENS.Include(p => p.Arma).FirstOrDefaultAsync(p => p.Id == x.Id);

                Personagem? oponente = await _context.TB_PERSONAGENS.FirstOrDefaultAsync(p => p.Id == x.OponenteId);

                int dano = atacante.Arma.Dano + (new Random().Next(atacante.Forca));

                dano = dano - new Random().Next(oponente.Defesa);

                if (dano > 0)
                    oponente.PontosVida = oponente.PontosVida - (int)dano;
                if (oponente.PontosVida < 0)
                    x.Narracao = $"{oponente.Nome} foi derrotado !";

                _context.TB_PERSONAGENS.Update(oponente);
                await _context.SaveChangesAsync();

                StringBuilder dados = new StringBuilder();

                dados.AppendFormat(" Atacante: {0}.", atacante.Nome);
                dados.AppendFormat("Oponente {0}", oponente.Nome);
                dados.AppendFormat(" Pontos de vida  de atacante: {0}.", atacante.PontosVida);
                dados.AppendFormat(" Pontos de do Oponente: {0}", oponente.Nome);
                dados.AppendFormat(" Arma Utilizado: {0}", atacante.Arma.Nome);
                dados.AppendFormat(" Dano {0}", dano);

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


        [HttpPost("DisuputaEmGrupo")]

        public async Task<IActionResult> DisputaEmGrupo(Disputa x)

        {
            try
            {
                x.Resultado = new List<string>();

                List<Personagem> personagens = await _context.TB_PERSONAGENS.Include(p => p.Arma).Include(p => p.PersonagemHabilidades).ThenInclude(px => px.Habilidade).Where(p => x.ListaIdPersonagens.Contains(p.Id)).ToListAsync();

                int qtdPersonagemVivos = personagens.FindAll(p => p.PontosVida > 0).Count;

                while (qtdPersonagemVivos > 1)
                {
                    List<Personagem> atacantes = personagens.Where(p => p.PontosVida > 0).ToList();
                    Personagem atacante = atacantes[new Random().Next(atacantes.Count)];
                    x.AtacanteId = atacante.Id;

                    List<Personagem> oponentes = personagens.Where(p => p.PontosVida > 0).ToList();
                    Personagem oponente = oponentes[new Random().Next(oponentes.Count)];
                    x.OponenteId = oponente.Id;

                    int dano = 0;
                    string ataqueUsado = string.Empty;
                    string resultado = string.Empty;

                    bool ataqueUsaArma = (new Random().Next(1) == 0);

                    if (ataqueUsaArma && atacante.Arma != null)
                    {

                        dano = atacante.Arma.Dano + (new Random().Next(atacante.Forca));
                        dano = dano - new Random().Next(oponente.Defesa);
                        ataqueUsado = atacante.Arma.Nome;

                        if (dano > 0)
                            oponente.PontosVida = oponente.PontosVida - (int)dano;

                        resultado = string.Format("{0} atacou {1} usado {2} com o dano {3}", atacante.Nome, oponente.Nome, ataqueUsado, dano);
                        x.Narracao += resultado;
                        x.Resultado.Add(resultado);


                    }
                    else if (atacante.PersonagemHabilidades.Count != 0)
                    {

                        int sorteioHabilidadeId = new Random().Next(atacante.PersonagemHabilidades.Count);
                        Habilidade habilidadeEscolhida = atacante.PersonagemHabilidades[sorteioHabilidadeId].Habilidade;
                        ataqueUsado = habilidadeEscolhida.Nome;

                        dano = habilidadeEscolhida.Dano + (new Random().Next(atacante.Inteligencia));
                        dano = dano - new Random().Next(oponente.Defesa);

                        if (dano > 0)
                            oponente.PontosVida = oponente.PontosVida - (int)dano;

                        resultado = string.Format("{0} atacou {1} usado {2} com o dano {3}", atacante.Nome, oponente.Nome, ataqueUsado, dano);
                        x.Narracao += resultado;
                        x.Resultado.Add(resultado);



                    }

                    if (!string.IsNullOrEmpty(ataqueUsado))
                    {
                        atacante.Vitorias++;
                        atacante.Derrotas++;
                        atacante.Disputas++;

                        x.Id = 0;
                        x.DataDisputa = DateTime.Now;
                        _context.TB_DISPUTA.Add(x);
                        await _context.SaveChangesAsync();

                    }

                    qtdPersonagemVivos = personagens.FindAll(p => p.PontosVida > 0).Count;

                    if (qtdPersonagemVivos == 1)
                    {
                        string resultadoFinal = $"{atacante.Nome.ToUpper()} é CAMPEÃO com {atacante.PontosVida} pontos de vidarestantes!";

                        x.Narracao += resultadoFinal;
                        x.Resultado.Add(resultadoFinal);

                        break;
                    }

                }

                _context.TB_PERSONAGENS.UpdateRange(personagens);
                await _context.SaveChangesAsync();

                return Ok(x);


            }

            catch (System.Exception ex)
            {
                return BadRequest(ex.Message)
            ;





            }
        }

        [HttpDelete("ApagarDisputas")]
        public async Task<IActionResult> DeleteAsync()
        {
            try
            {
                List<Disputa> disputas = await _context.TB_DISPUTA.ToListAsync(); _context.TB_DISPUTA.RemoveRange(disputas);
                await _context.SaveChangesAsync();
                return Ok("Disputas apagadas");
            }
            catch (System.Exception ex)
            { return BadRequest(ex.Message); }
        }

        [HttpGet("Listar")]
        public async Task<IActionResult> ListarAsync()
        {
            try
            {
                List<Disputa> disputas =
                await _context.TB_DISPUTA.ToListAsync();
                return Ok(disputas);
            }
            catch (System.Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
 
        
}
}


