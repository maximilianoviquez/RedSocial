using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dominio;
using Microsoft.AspNetCore.Mvc;


namespace WebApp.Controllers
{
    public class UsuarioController : Controller
    {

        Sistema s = Sistema.GetInstancia();

        public IActionResult Index()
        {
            int? lid = HttpContext.Session.GetInt32("LogueadoId");
            if (lid == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Obtener la lista de posts no baneados y visibles
            var posts = s.GetPosts();

            return View(posts);
        }

        public IActionResult Registro()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Registro(Miembro m)
        {
            try
            {
                m.EsValido();
                s.AltaUsuario(m);
                ViewBag.msg = "Alta correcta";
                return RedirectToAction("Login", "Usuario");
            }
            catch (Exception e)
            {
                ViewBag.msg = e.Message;
            }
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string pass)
        {
            if (email!=null && pass !=null)
            {
                Usuario buscado = s.ExisteUsuario(email, pass);
                if (buscado != null)
                {
                    HttpContext.Session.SetInt32("LogueadoId", buscado.Id);
                    HttpContext.Session.SetString("LogueadoEmail", buscado.Email);
                    HttpContext.Session.SetString("LogueadoRol", buscado.GetType().Name);
                    if(buscado is Miembro aux)
                    {
                        HttpContext.Session.SetString("LogueadoNombre", aux.Nombre.ToString());
                        HttpContext.Session.SetString("LogueadoApellido", aux.Apellido.ToString());
                        HttpContext.Session.SetString("LogueadoFechaNac", aux.FechaNac.ToString());

                    }


                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ViewBag.msg = "Datos Incorrectos";
                    return View();
                }
            }
            else
            {
                ViewBag.msg = "Algun campo esta vacio";
                return View();
            }
        }


        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult Lista()
        {
            int? lid = HttpContext.Session.GetInt32("LogueadoId");
            if(lid == null)
            {
                return RedirectToAction("Index","Home");
            }

            var miembros = s.GetUsuarios().OfType<Dominio.Miembro>().ToList();
            return View(miembros);
        }

        public IActionResult PerfilMiembro()
        {
            string? lrol = HttpContext.Session.GetString("LogueadoRol");
            if (lrol != "Miembro")
            {
                return RedirectToAction("Index", "Home");
            }
            int? lid = HttpContext.Session.GetInt32("LogueadoId");
            Usuario logueado = s.BuscarUsuario(lid);
            return View(logueado);
        }


        public IActionResult BloquearUsuario(int id)
        {
            int? lid = HttpContext.Session.GetInt32("LogueadoId");
            if (lid == null)
            {
                return RedirectToAction("Index", "Home");
            }

            Usuario logueado = s.BuscarUsuario(lid);

            if (logueado is Administrador admin)
            {
                Usuario usuarioABloquear = s.BuscarUsuario(id);
                if (usuarioABloquear != null)
                {
                    admin.BloquearUsuario(usuarioABloquear);
                    ViewBag.msg = $"El usuario {usuarioABloquear.Email} ha sido bloqueado.";
                }
                else
                {
                    ViewBag.msg = "Usuario no encontrado.";
                }
            }
            else
            {
                ViewBag.msg = "Acceso no autorizado.";
            }

            return RedirectToAction("Lista", "Usuario");
        }

        public IActionResult DesbloquearUsuario(int id)
        {
            int? lid = HttpContext.Session.GetInt32("LogueadoId");
            if (lid == null)
            {
                return RedirectToAction("Index", "Home");
            }

            Usuario logueado = s.BuscarUsuario(lid);

            if (logueado is Administrador admin)
            {
                Usuario usuarioADesbloquear = s.BuscarUsuario(id);
                if (usuarioADesbloquear != null)
                {
                    admin.DesBloquearUsuario(usuarioADesbloquear);
                    ViewBag.msg = $"El usuario {usuarioADesbloquear.Email} ha sido desbloqueado.";
                }
                else
                {
                    ViewBag.msg = "Usuario no encontrado.";
                }
            }
            else
            {
                ViewBag.msg = "Acceso no autorizado.";
            }

            return RedirectToAction("Lista", "Usuario");
        }

        public IActionResult ListaMiembros()
        {
            int? lid = HttpContext.Session.GetInt32("LogueadoId");
            if (lid == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Obtener la lista de miembros
            var miembros = s.GetUsuarios().OfType<Dominio.Miembro>().ToList();
            ViewBag.Miembros = miembros;

            // Obtener las solicitudes pendientes solo para el miembro logueado
            var solicitudesPendientes = s.GetInvitaciones()
                .Where(inv => inv.MiembroSolicitado.Id == lid && inv.Estado == TipoEstado.Pendiente)
                .ToList();
            ViewBag.SolicitudesPendientes = solicitudesPendientes;

            return View(miembros);
        }


        public IActionResult Solicitud()
        {
            int? lid = HttpContext.Session.GetInt32("LogueadoId");
            if (lid == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Obtener la lista de miembros registrados (excepto el propio miembro)
            var miembros = s.GetUsuarios().OfType<Dominio.Miembro>().Where(m => m.Id != lid).ToList();
            ViewBag.Miembros = miembros;

            // Obtener las solicitudes pendientes solo para el miembro logueado
            var solicitudesPendientes = s.GetInvitaciones()
                .Where(inv => inv.MiembroSolicitado.Id == lid && inv.Estado == TipoEstado.Pendiente)
                .ToList();
            ViewBag.SolicitudesPendientes = solicitudesPendientes;

            return View();
        }

        [HttpPost]
        public IActionResult EnviarSolicitud(int idAmigo)
        {
            int? lid = HttpContext.Session.GetInt32("LogueadoId");

            if (lid == null)
            {
                return RedirectToAction("Index", "Home");
            }

            try
            {
                Miembro logueado = s.BuscarUsuario(lid) as Miembro;
                if (logueado != null)
                {
                    Miembro amigo = s.BuscarUsuario(idAmigo) as Miembro;

                    if (amigo != null && !s.SonAmigos(logueado, amigo))
                    {
                        // Verificar si ya existe una solicitud pendiente o aceptada
                        if (!s.GetInvitaciones().Any(inv =>
                            (inv.MiembroSolicitante == logueado && inv.MiembroSolicitado == amigo) ||
                            (inv.MiembroSolicitante == amigo && inv.MiembroSolicitado == logueado) &&
                            (inv.Estado == TipoEstado.Pendiente || inv.Estado == TipoEstado.Aceptada)))
                        {
                            Invitacion nuevaSolicitud = new Invitacion(logueado, amigo);
                            s.AltaInvitacion(nuevaSolicitud);
                            ViewBag.SuccessMessage = "Solicitud de amistad enviada correctamente.";
                        }
                        else
                        {
                            ViewBag.ErrorMessage = "Ya existe una solicitud pendiente o son amigos.";
                        }
                    }
                    else
                    {
                        ViewBag.ErrorMessage = "No se puede enviar la solicitud.";
                    }
                }
                else
                {
                    ViewBag.ErrorMessage = "Usuario no encontrado.";
                }
            }
            catch (Exception e)
            {
                ViewBag.ErrorMessage = e.Message;
            }

            // Obtener la lista de miembros y solicitudes pendientes
            var miembros = s.GetUsuarios().OfType<Dominio.Miembro>().Where(m => m.Id != lid).ToList();
            ViewBag.Miembros = miembros;

            var solicitudesPendientes = s.GetInvitaciones()
                .Where(inv => inv.MiembroSolicitado.Id == lid && inv.Estado == TipoEstado.Pendiente)
                .ToList();
            ViewBag.SolicitudesPendientes = solicitudesPendientes;

            return View("Solicitud");
        }


        [HttpPost]
        public IActionResult AceptarSolicitud(int idInvitacion)
        {
            try
            {
                Invitacion invitacion = s.GetInvitaciones().FirstOrDefault(inv => inv.Id == idInvitacion);

                if (invitacion != null)
                {
                    invitacion.Aceptar();
                    TempData["SuccessMessage"] = "Solicitud aceptada correctamente.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Invitación no encontrada.";
                }
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = e.Message;
            }

            return RedirectToAction("Solicitud");
        }

        [HttpPost]
        public IActionResult RechazarSolicitud(int idInvitacion)
        {
            try
            {
                Invitacion invitacion = s.GetInvitaciones().FirstOrDefault(inv => inv.Id == idInvitacion);

                if (invitacion != null)
                {
                    invitacion.Rechazar();
                    TempData["SuccessMessage"] = "Solicitud rechazada correctamente.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Invitación no encontrada.";
                }
            }
            catch (Exception e)
            {
                TempData["ErrorMessage"] = e.Message;
            }

            return RedirectToAction("Solicitud");
        }









    }

}

