using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Balu0._1.Models;
using ImageResizer;
using Microsoft.Ajax.Utilities;

namespace Balu0._1.Controllers
{
    public class HomeController : Controller
    {
        private BaluEntities db = new BaluEntities();

        public ActionResult Index(string searchString, string startData, string startStatus, string empty = "", bool emb = false, int idOne = 1)
        {
            //Tabelas
            bool startOp = Convert.ToBoolean(emb);
            var order = db.Programa.Where(x => x.Embarque == startOp).ToList();// Apresenta Embarque em False
            var coler = db.Programa_Cor.Where(x => x.ID_Programa_Malha == idOne).ToList();// Apresenta Quantidades com valor Malha 1
            var promalha = db.Programa_Malha.Where(x => x.ID_Programa_Malha == idOne).ToList();// Apresenta Referencias com valor Malha 1
            var precos = db.Preco_Malha.Where(x => x.ID_Preco_Malha == idOne).DistinctBy(m => m.Ref_Balu).ToList();// Apresenta preço_malha com valor Malha 1
            var quant = db.Programa_Cor_Info.ToList();
            var statu = db.Programa_Cor_Info_Status.ToList();
            var malha = db.Malha.ToList();


            //Join
            var result1 = (from m in malha
                           join p in precos on m.ID_Ref equals p.ID_Ref into malhapreco
                           from mp in malhapreco
                           join o in order on mp.Ref_Balu equals o.Ref_Balu into malhaprcOrders
                           from mpo in malhaprcOrders
                           join pm in promalha on mpo.ID_Programa equals pm.ID_Programa into allOrder
                           from ao in allOrder
                           select new OrdersColorsViewModel
                           {
                               malha = m,
                               preco = mp,
                               order = mpo,
                               promalha = allOrder.ToList(),
                           }).ToList();

            var result2 = (from c in coler
                           join q in quant on new { orderId = c.ID_Programa, colorlineId = c.ID_Linha_Cor } equals new { orderId = q.ID_Programa, colorlineId = q.ID_Linha_Cor } into orderColerQuant
                           from ocq in orderColerQuant
                           join s in statu on new { orderId = ocq.ID_Programa, colorlineId = ocq.ID_Linha_Cor } equals new { orderId = s.ID_Programa, colorlineId = s.ID_Linha_Cor } into orderColerQuantStat
                           //from ocqs in orderColerQuantStat
                           group new ColorsAndQuantities
                           {
                               coler = c,
                               quant = orderColerQuant.ToList(),
                               status = orderColerQuantStat.ToList(),
                           } by c.ID_Programa).ToList();

            var result = (from r1 in result1
                          join r2 in result2 on r1.order.ID_Programa equals r2.Key
                          select new OrdersColorsViewModel
                          {
                              order = r1.order,
                              malha = r1.malha,
                              preco = r1.preco,
                              promalha = r1.promalha,
                              colers = r2.ToList(),
                          }).ToList().OrderBy(s => s.order.Semana_Embarque);


            //Dropdownlist Data
            var listQry = from d in result
                          orderby d.order.Semana_Embarque
                          select d.order.Semana_Embarque.Value.ToString("dd/MM/yyyy");
            var listLst = listQry.Distinct();
            ViewBag.startData = new SelectList(listLst);

            //Dropdownlist Status
            ViewBag.startStatus = new List<SelectListItem>
                                                 {
                                               new SelectListItem { Text = "Sem malha", Value = "Sem malha"},
                                               new SelectListItem { Text = "No gabinete (dos moldes)", Value = "No gabinete (dos moldes)"},
                                               new SelectListItem { Text = "A testar corte", Value = "A testar corte"},
                                               new SelectListItem { Text = "A bordar", Value = "A bordar"},
                                               new SelectListItem { Text = "Para cortar", Value = "Para cortar"},
                                               new SelectListItem { Text = "Em corte", Value = "Em corte"},
                                               new SelectListItem { Text = "A estampar", Value = "A estampar"},
                                               new SelectListItem { Text = "Para colocar", Value = "Para colocar"},
                                               new SelectListItem { Text = "Em confeção", Value = "Em confeção"},
                                               new SelectListItem { Text = "Para aparar", Value = "Para aparar"},
                                               new SelectListItem { Text = "Para lavar", Value = "Para lavar"},
                                               new SelectListItem { Text = "Para tingir", Value = "Para tingir"},
                                               new SelectListItem { Text = "Para ferros", Value = "Para ferros"},
                                               new SelectListItem { Text = "Para arranjos", Value = "Para arranjos"},
                                               new SelectListItem { Text = "Para embalagem", Value = "Para embalagem"},
                                               new SelectListItem { Text = "Embalado", Value = "Embalado"},
                                               new SelectListItem { Text = "Para controle", Value = "Para controle"},
                                               new SelectListItem { Text = "Aguarda aprovação cliente", Value = "Aguarda aprovação cliente"},
                                                   };

            ViewBag.countt = result.Count();

            //ViewBag.totaStatVB = result.Sum(a => a.colers[0].quant[0].Cliente);

            var totalsum = result.Select(m => m.colers.Select(c => new {
                Clientelist = c.quant.Select(i => i.Cliente).ToList(),
                status = c.status.Where(n => n.Status != empty ).ToList().ToList()
            }).ToList())
             .Select(m => m.Where(i => i.status.Count() != 0).Sum(p => p.Clientelist.Sum()))
             .ToList();
            ViewBag.totaStatVB = string.Join(", ", totalsum.Where(a => a.Value != 0).Sum());

            var statVazio = result.Select(m => m.colers.Select(c => new {
                Clientelist = c.quant.Select(i => i.Cliente).ToList(),
                status = c.status.Where(n => n.Status == null).ToList().ToList()
            }).ToList())
             .Select(m => m.Where(i => i.status.Count() != 0).Sum(p => p.Clientelist.Sum()))
             .ToList();
            ViewBag.statVazioVB = string.Join(", ", statVazio.Where(a => a.Value != 0).Sum());


            //
            var smalha = result.Select(m => m.colers.Select(c => new {
                Clientelist = c.quant.Select(i => i.Cliente).ToList(),
                status = c.status.Where(n => n.Status == "Sem malha").ToList().ToList()
            }).ToList())
             .Select(m => m.Where(i => i.status.Count() != 0).Sum(p => p.Clientelist.Sum()))
             .ToList();
            ViewBag.smalhaVB = string.Join(", ", smalha.Where(a => a.Value != 0).Sum());


            //
            var nogab = result.Select(m => m.colers.Select(c => new {
                Clientelist = c.quant.Select(i => i.Cliente).ToList(),
                status = c.status.Where(n => n.Status == "No gabinete (dos moldes)").ToList().ToList()
            }).ToList())
             .Select(m => m.Where(i => i.status.Count() != 0).Sum(p => p.Clientelist.Sum()))
             .ToList();
            ViewBag.nogabVB = string.Join(", ", nogab.Where(a => a.Value != 0).Sum());


            //
            var testcort = result.Select(m => m.colers.Select(c => new {
                Clientelist = c.quant.Select(i => i.Cliente).ToList(),
                status = c.status.Where(n => n.Status == "A testar corte").ToList().ToList()
            }).ToList())
             .Select(m => m.Where(i => i.status.Count() != 0).Sum(p => p.Clientelist.Sum()))
             .ToList();
            ViewBag.testcortVB = string.Join(", ", testcort.Where(a => a.Value != 0).Sum());

            //
            var abordar = result.Select(m => m.colers.Select(c => new {
                Clientelist = c.quant.Select(i => i.Cliente).ToList(),
                status = c.status.Where(n => n.Status == "A bordar").ToList().ToList()
            }).ToList())
             .Select(m => m.Where(i => i.status.Count() != 0).Sum(p => p.Clientelist.Sum()))
             .ToList();
            ViewBag.abordarVB = string.Join(", ", abordar.Where(a => a.Value != 0).Sum());

            //
            var pcortar = result.Select(m => m.colers.Select(c => new {
                Clientelist = c.quant.Select(i => i.Cliente).ToList(),
                status = c.status.Where(n => n.Status == "Para cortar").ToList().ToList()
            }).ToList())
             .Select(m => m.Where(i => i.status.Count() != 0).Sum(p => p.Clientelist.Sum()))
             .ToList();
            ViewBag.pcortarVB = string.Join(", ", pcortar.Where(a => a.Value != 0).Sum());

            //
            var emcorte = result.Select(m => m.colers.Select(c => new {
                Clientelist = c.quant.Select(i => i.Cliente).ToList(),
                status = c.status.Where(n => n.Status == "Em corte").ToList().ToList()
            }).ToList())
             .Select(m => m.Where(i => i.status.Count() != 0).Sum(p => p.Clientelist.Sum()))
             .ToList();
            ViewBag.emcorteVB = string.Join(", ", emcorte.Where(a => a.Value != 0).Sum());

            //
            var aestamp = result.Select(m => m.colers.Select(c => new {
                Clientelist = c.quant.Select(i => i.Cliente).ToList(),
                status = c.status.Where(n => n.Status == "A estampar").ToList().ToList()
            }).ToList())
             .Select(m => m.Where(i => i.status.Count() != 0).Sum(p => p.Clientelist.Sum()))
             .ToList();
            ViewBag.aestampVB = string.Join(", ", aestamp.Where(a => a.Value != 0).Sum());

            //
            var parcolocar = result.Select(m => m.colers.Select(c => new {
                Clientelist = c.quant.Select(i => i.Cliente).ToList(),
                status = c.status.Where(n => n.Status == "Para colocar").ToList().ToList()
            }).ToList())
             .Select(m => m.Where(i => i.status.Count() != 0).Sum(p => p.Clientelist.Sum()))
             .ToList();
            ViewBag.parcolocarVB = string.Join(", ", parcolocar.Where(a => a.Value != 0).Sum());

            //
            var emconfec = result.Select(m => m.colers.Select(c => new {
                Clientelist = c.quant.Select(i => i.Cliente).ToList(),
                status = c.status.Where(n => n.Status == "Em confeção").ToList().ToList()
            }).ToList())
             .Select(m => m.Where(i => i.status.Count() != 0).Sum(p => p.Clientelist.Sum()))
             .ToList();
            ViewBag.emconfecVB = string.Join(", ", emconfec.Where(a => a.Value != 0).Sum());

            //
            var paraAparar = result.Select(m => m.colers.Select(c => new {
                Clientelist = c.quant.Select(i => i.Cliente).ToList(),
                status = c.status.Where(n => n.Status == "Para aparar").ToList().ToList()
            }).ToList())
             .Select(m => m.Where(i => i.status.Count() != 0).Sum(p => p.Clientelist.Sum()))
             .ToList();
            ViewBag.paraApararVB = string.Join(", ", paraAparar.Where(a => a.Value != 0).Sum());

            //
            var paraLavar = result.Select(m => m.colers.Select(c => new {
                Clientelist = c.quant.Select(i => i.Cliente).ToList(),
                status = c.status.Where(n => n.Status == "Para lavar").ToList().ToList()
            }).ToList())
             .Select(m => m.Where(i => i.status.Count() != 0).Sum(p => p.Clientelist.Sum()))
             .ToList();
            ViewBag.paraLavarVB = string.Join(", ", paraLavar.Where(a => a.Value != 0).Sum());

            //
            var paratingir = result.Select(m => m.colers.Select(c => new {
                Clientelist = c.quant.Select(i => i.Cliente).ToList(),
                status = c.status.Where(n => n.Status == "Para tingir").ToList().ToList()
            }).ToList())
             .Select(m => m.Where(i => i.status.Count() != 0).Sum(p => p.Clientelist.Sum()))
             .ToList();
            ViewBag.paratingirVB = string.Join(", ", paratingir.Where(a => a.Value != 0).Sum());

            //
            var paraferros = result.Select(m => m.colers.Select(c => new {
                Clientelist = c.quant.Select(i => i.Cliente).ToList(),
                status = c.status.Where(n => n.Status == "paraferros").ToList().ToList()
            }).ToList())
             .Select(m => m.Where(i => i.status.Count() != 0).Sum(p => p.Clientelist.Sum()))
             .ToList();
            ViewBag.paraferrosVB = string.Join(", ", paraferros.Where(a => a.Value != 0).Sum());

            //
            var paraArranjos = result.Select(m => m.colers.Select(c => new {
                Clientelist = c.quant.Select(i => i.Cliente).ToList(),
                status = c.status.Where(n => n.Status == "Para arranjos").ToList().ToList()
            }).ToList())
             .Select(m => m.Where(i => i.status.Count() != 0).Sum(p => p.Clientelist.Sum()))
             .ToList();
            ViewBag.paraArranjosVB = string.Join(", ", paraArranjos.Where(a => a.Value != 0).Sum());

            //
            var paraEmbala = result.Select(m => m.colers.Select(c => new {
                Clientelist = c.quant.Select(i => i.Cliente).ToList(),
                status = c.status.Where(n => n.Status == "Para embalagem").ToList().ToList()
            }).ToList())
             .Select(m => m.Where(i => i.status.Count() != 0).Sum(p => p.Clientelist.Sum()))
             .ToList();
            ViewBag.paraEmbalaVB = string.Join(", ", paraEmbala.Where(a => a.Value != 0).Sum());

            //
            var embalad = result.Select(m => m.colers.Select(c => new {
                Clientelist = c.quant.Select(i => i.Cliente).ToList(),
                status = c.status.Where(n => n.Status == "Embalado").ToList().ToList()
            }).ToList())
             .Select(m => m.Where(i => i.status.Count() != 0).Sum(p => p.Clientelist.Sum()))
             .ToList();
            ViewBag.embaladVB = string.Join(", ", embalad.Where(a => a.Value != 0).Sum());

            //
            var paraControl = result.Select(m => m.colers.Select(c => new {
                Clientelist = c.quant.Select(i => i.Cliente).ToList(),
                status = c.status.Where(n => n.Status == "Para controle").ToList().ToList()
            }).ToList())
             .Select(m => m.Where(i => i.status.Count() != 0).Sum(p => p.Clientelist.Sum()))
             .ToList();
            ViewBag.paraControlVB = string.Join(", ", paraControl.Where(a => a.Value != 0).Sum());

            //
            var aguardAprov = result.Select(m => m.colers.Select(c => new {
                Clientelist = c.quant.Select(i => i.Cliente).ToList(),
                status = c.status.Where(n => n.Status == "Aguarda aprovação cliente").ToList().ToList()
            }).ToList())
             .Select(m => m.Where(i => i.status.Count() != 0).Sum(p => p.Clientelist.Sum()))
             .ToList();
            ViewBag.aguardAprovVB = string.Join(", ", aguardAprov.Where(a => a.Value != 0).Sum());

            // Barra de procura 
            var proResult = from p in result
                            select p;

            if (!String.IsNullOrEmpty(searchString))
            {
                if (startData == empty && startStatus == empty)
                {
                    string upper = searchString.ToUpper();
                    proResult = proResult.Where(x => x.order.Modelo.Contains(upper) || x.order.Colecao.Contains(upper) || x.order.Num_Encomenda.Contains(upper) || x.order.Ref_Cliente.Contains(upper) || x.order.Cod_Artigo.Contains(upper) || x.malha.Ref_Malha.Contains(upper) || x.malha.Nome_Malha.Contains(upper));
                    return View(proResult);
                }
                if (startData != empty && startStatus == empty)
                {
                    DateTime starttt = Convert.ToDateTime(startData);
                    string upper = searchString.ToUpper();
                    var proResultt = proResult.Where(x => x.order.Modelo.Contains(upper) || x.order.Colecao.Contains(upper) || x.order.Num_Encomenda.Contains(upper) || x.order.Ref_Cliente.Contains(upper) || x.order.Cod_Artigo.Contains(upper) || x.malha.Ref_Malha.Contains(upper) || x.malha.Nome_Malha.Contains(upper))
                        .Where(x => x.order.Semana_Embarque == starttt);

                    return View(proResultt);
                }
                if (startData == empty && startStatus != empty)
                {
                    string upper = searchString.ToUpper();
                    var proResulttt = proResult.Where(x => x.order.Modelo.Contains(upper) || x.order.Colecao.Contains(upper) || x.order.Num_Encomenda.Contains(upper) || x.order.Ref_Cliente.Contains(upper) || x.order.Cod_Artigo.Contains(upper) || x.malha.Ref_Malha.Contains(upper) || x.malha.Nome_Malha.Contains(upper))
                                          .Where(x => x.colers[0].status[0].Status == startStatus);
                    return View(proResulttt);
                }
                if (startData != empty && startStatus != empty)
                {
                    DateTime startttt = Convert.ToDateTime(startData);
                    string upper = searchString.ToUpper();
                    var proResultttt = proResult.Where(x => x.order.Modelo.Contains(upper) || x.order.Colecao.Contains(upper) || x.order.Num_Encomenda.Contains(upper) || x.order.Ref_Cliente.Contains(upper) || x.order.Cod_Artigo.Contains(upper) || x.malha.Ref_Malha.Contains(upper) || x.malha.Nome_Malha.Contains(upper))
                                          .Where(x => x.order.Semana_Embarque == startttt)
                                          .Where(x => x.colers[0].status[0].Status == startStatus);
                    return View(proResultttt);
                }
                return View(proResult);
            }

            // Validação do valor inserido na dropdownlist data
            if (string.IsNullOrEmpty(startData))
            {
                if (startStatus == empty)
                {
                    return View(result);
                }
                if (startData != null)
                {
                    var procuraaaa = result.Where(x => x.colers[0].status[0].Status == startStatus);

                    return View(procuraaaa);
                }
                if (startStatus != empty)
                {
                    return View(result);
                }

                return View(result);
            }
            else
            {
                DateTime start = Convert.ToDateTime(startData);
                var procuraa = result.Where(x => x.order.Semana_Embarque == start);


                if (startStatus != empty)
                {
                    DateTime startt = Convert.ToDateTime(startData);
                    var procuraaa = result.Where(x => x.order.Semana_Embarque == startt)
                                          .Where(x => x.colers[0].status[0].Status == startStatus);
                    return View(procuraaa);
                }

                return View(procuraa);
                //return View(result.Where(x => x.order.Semana_Embarque == start));
            }

        }
        public ActionResult FileUpload(HttpPostedFileBase file)
        {

            if (file != null)
            {
                string pic = System.IO.Path.GetFileName(file.FileName);
                string path = System.IO.Path.Combine(
                                       Server.MapPath("~/Img/"), pic);

                // file is uploaded
                file.SaveAs(path);
                //Instalar ImageResizer patch
                new ResizeSettings("maxwidth=300&maxheight=300");
                ResizeSettings resizeSetting = new ResizeSettings
                {
                    Width = 300,
                    Height = 300,
                    Format = "jpg"
                };
                ImageBuilder.Current.Build(path, path, resizeSetting);

                // save the image path path to the database or you can send image 
                // directly to database
                // in-case if you want to store byte[] ie. for DB
                using (MemoryStream ms = new MemoryStream())
                {
                    file.InputStream.CopyTo(ms);
                    byte[] array = ms.GetBuffer();
                }

            }
            // after successfully uploading redirect the user
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public JsonResult EditPost(Programa_Cor_Info_Status statusData)
        {

            Programa_Cor_Info_Status status = new Programa_Cor_Info_Status()
            {
                ID_Programa = statusData.ID_Programa,
                ID_Linha_Cor = statusData.ID_Linha_Cor,
                ID_Programa_Malha = statusData.ID_Programa_Malha,
                Talao = statusData.Talao,
                Status = statusData.Status,
                Obs = statusData.Obs,
                Data_mod = statusData.Data_mod,
            };
            db.Entry(status).State = EntityState.Modified;
            db.SaveChanges();
            return Json(status, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult AddPost(Programa_Cor_Info infoData)
        {

            Programa_Cor_Info_Status info = new Programa_Cor_Info_Status()
            {
                ID_Programa = infoData.ID_Programa,
                ID_Linha_Cor = infoData.ID_Linha_Cor,
                ID_Programa_Malha = infoData.ID_Programa_Malha,
            };

            db.Entry(info).State = EntityState.Added;
            db.SaveChanges();
            return Json(info, JsonRequestBehavior.AllowGet);
        }
    }
}

