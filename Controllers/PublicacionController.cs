using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Dominio;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;



namespace WebApp.Controllers
{
    public class PublicacionController : Controller
    {
        Sistema s = Sistema.GetInstancia();

        private IWebHostEnvironment Environment;

        public PublicacionController(IWebHostEnvironment _enviorment)
        {
            Environment = _enviorment;
        }

        //Buscar Post/Coment
        public IActionResult Busqueda()
        {
            string lrol = HttpContext.Session.GetString("LogueadoRol");
            if(lrol != "Miembro")
            {
                return RedirectToAction("Index","Home");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Busqueda(string textobuscar, int numbuscar)
        {
            IEnumerable<Publicacion> listafiltrada = s.GetPostsPorTextoYNum(textobuscar, numbuscar);
            IEnumerable<Post> postsFiltrados = listafiltrada.OfType<Post>(); 

            return View(postsFiltrados);
        }

        //Subir Post
        public IActionResult Create()
        {

            if(HttpContext.Session.GetInt32("LogueadoId")!= null && HttpContext.Session.GetString("LogueadoRol") == "Miembro")
            {
                return View();
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public IActionResult Create(string Titulo, IFormFile archivo, string Contenido, bool EsPublico)
        {
            int? lid = HttpContext.Session.GetInt32("LogueadoId");

            if (lid != null)
            {
                Miembro autor = s.MiembroPorId(lid);

                if (autor != null && archivo != null && !autor.Bloqueado)
                {
                    try
                    {
                        Post p = new(autor, Titulo, Contenido)
                        {
                            EsPublico = EsPublico
                        };

                        string ruta = Environment.WebRootPath + "//img//";
                        string extension = Path.GetExtension(archivo.FileName);
                        string fileName = p.Id.ToString() + extension;

                        using (FileStream stream = new FileStream(ruta + fileName, FileMode.Create))
                        {
                            archivo.CopyTo(stream);
                        }

                        p.Imagen = fileName;
                        s.AltaPublicacion(p);

                        ViewBag.Resultado = "Post creado correctamente.";
                    }
                    catch (Exception ex)
                    {
                        ViewBag.Resultado = "Error al crear el post: " + ex.Message;
                    }
                }
                else
                {
                    ViewBag.Resultado = "Error, verifique los campos";
                }
            }
            else
            {
                ViewBag.Resultado = "Usuario no encontrado o bloqueado.";
            }

            return View();
        }

        // Reaccionar
        public IActionResult DarReaccion(int idPost, string reaction)
        {
            int? idUsuario = HttpContext.Session.GetInt32("LogueadoId");

            if (idUsuario != null)
            {
                try
                {
                    Miembro usuario = s.MiembroPorId(idUsuario.Value);
                    Post publicacion = s.GetPostPorId(idPost);

                    if (usuario != null && publicacion != null)
                    {
                        TipoReaccion tipoReaccion = (reaction.ToLower() == "like") ? TipoReaccion.Like : TipoReaccion.Dislike;

                        try
                        {
                            publicacion.Reaccionar(usuario, tipoReaccion);
                            TempData[$"MsgReaccion{reaction}_{idPost}"] = $"¡Has dado {reaction} correctamente!";
                        }
                        catch (Exception ex)
                        {
                            TempData[$"MsgReaccion{reaction}_{idPost}"] = $"Error al dar {reaction}: {ex.Message}";
                        }
                    }
                    else
                    {
                        TempData[$"MsgReaccion{reaction}_{idPost}"] = "No se encontró la publicación o el usuario con los IDs proporcionados.";
                    }
                }
                catch (Exception ex)
                {
                    TempData[$"MsgReaccion{reaction}_{idPost}"] = $"Error al dar {reaction}: {ex.Message}";
                }
            }
            else
            {
                TempData[$"MsgReaccion{reaction}_{idPost}"] = "Usuario no autenticado.";
            }

            return RedirectToAction("Index");
        }

        //dar Like
        public IActionResult DarLike(int idPost)
        {
            return DarReaccion(idPost, "like");
        }
        //dar dislike
        public IActionResult DarDislike(int idPost)
        {
            return DarReaccion(idPost, "dislike");
        }

        // Reaccionar a comentario
        public IActionResult DarReaccionComentario(int idComentario, string reaction)
        {
            int? idUsuario = HttpContext.Session.GetInt32("LogueadoId");

            if (idUsuario != null)
            {
                try
                {
                    Miembro usuario = s.MiembroPorId(idUsuario.Value);
                    Comentario comentario = s.GetComentarioPorId(idComentario);

                    if (usuario != null && comentario != null)
                    {
                        TipoReaccion tipoReaccion = (reaction.ToLower() == "like") ? TipoReaccion.Like : TipoReaccion.Dislike;

                        try
                        {
                            comentario.Reaccionar(usuario, tipoReaccion);
                            TempData[$"MsgComentario_{idComentario}"] = $"¡Has dado {reaction} correctamente al comentario!";
                        }
                        catch (Exception ex)
                        {
                            TempData[$"MsgComentario_{idComentario}"] = $"Error al dar {reaction} al comentario: {ex.Message}";
                        }
                    }
                    else
                    {
                        TempData[$"MsgComentario_{idComentario}"] = "No se encontró el comentario o el usuario con los IDs proporcionados.";
                    }
                }
                catch (Exception ex)
                {
                    TempData[$"MsgComentario_{idComentario}"] = $"Error al dar {reaction} al comentario: {ex.Message}";
                }
            }
            else
            {
                TempData[$"MsgComentario_{idComentario}"] = "Usuario no autenticado.";
            }

            return RedirectToAction("Index");
        }

        // Dar like a comentario
        public IActionResult DarLikeComentario(int idComentario)
        {
            return DarReaccionComentario(idComentario, "like");
        }

        // Dar dislike a comentario
        public IActionResult DarDislikeComentario(int idComentario)
        {
            return DarReaccionComentario(idComentario, "dislike");
        }

        //comentar 
        public IActionResult ComentarPost(string tituloContenidoNuevo, string contenidoNuevo, int idPost)
        {
            try
            {
                int? lid = HttpContext.Session.GetInt32("LogueadoId");
                Miembro autorComentario = s.MiembroPorId(lid);
                Post buscado = s.GetPostPorId(idPost);

                
                if (lid != null && autorComentario != null && buscado != null &&
                    (buscado.EsPublico || s.SonAmigos(buscado.Autor, autorComentario) || buscado.Autor == autorComentario) &&
                    !autorComentario.Bloqueado)
                {
                    Comentario comentarionuevo = new Comentario(false, DateTime.Now, autorComentario, tituloContenidoNuevo, contenidoNuevo);

                    
                    comentarionuevo.EsValido();

                    s.AgregarComentario(buscado, comentarionuevo);
                    TempData[$"MsgComentario_{comentarionuevo.Id}"] = "Comentario agregado correctamente.";
                }
                else
                {
                    TempData["MsgComentario"] = "No se pudo agregar el comentario. Verifica las condiciones.";
                }
            }
            catch (Exception ex)
            {
                TempData["MsgComentario"] = $"Error al agregar el comentario: {ex.Message}";
            }
            return View("Index", s.GetPosts());
        }

        public IActionResult Index()
        {
            if (HttpContext.Session.GetInt32("LogueadoId") != null && HttpContext.Session.GetString("LogueadoRol") == "Miembro")
            {
                int? idUsuarioLogueado = HttpContext.Session.GetInt32("LogueadoId");
                Miembro usuarioLogueado = s.MiembroPorId(idUsuarioLogueado);

                List<Post> listaFiltrada = s.GetPosts()
                    .Where(post => EstaHabilitadoParaUsuario(post, usuarioLogueado))
                    .ToList();

                return View(listaFiltrada);
            }

            return RedirectToAction("Index", "Home");
        }

        private bool EstaHabilitadoParaUsuario(Post post, Miembro usuarioLogueado)
        {
            return !post.Censurados && (post.EsPublico || post.Autor == usuarioLogueado || s.SonAmigos(post.Autor, usuarioLogueado));
        }

        //Muro de Usuario Administrador
        public IActionResult MuroAdmin()
        {

            if (HttpContext.Session.GetInt32("LogueadoId") != null && HttpContext.Session.GetString("LogueadoRol") == "Administrador")
            {
            ViewBag.AdministradorLogueadoId = ObtenerUsuarioAdministrador()?.Id;
            return View(s.GetPosts());
            }


            return RedirectToAction("Index", "Home");
        }


        [HttpPost]
        public IActionResult BanearPost(int idPost)
        {
            Usuario admin = ObtenerUsuarioAdministrador();
            Post post = s.GetPostPorId(idPost);

            if (post != null && admin != null)
            {
                if (!post.Censurados) 
                {
                    post.Censurados = true;
                    post.IdAdministradorQueBaneo = admin.Id;


                    ViewBag.Msg = "El post ha sido banneado por el administrador.";
                }
                else
                {
                    ViewBag.Msg = "El post ya estaba baneado.";
                }
            }

            return RedirectToAction("MuroAdmin");
        }

        public Usuario ObtenerUsuarioAdministrador()
        {
            foreach (Usuario usuario in s.GetUsuarios())
            {
                if (usuario is Administrador)
                {
                    return usuario;
                }
            }

            return null; // Si no se encuentra ningún administrador
        }







    }
}

