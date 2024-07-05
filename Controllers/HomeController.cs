using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using WebApp.Models;

namespace WebApp.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        int? LogueadoId = HttpContext.Session.GetInt32("LogueadoId");

        if (LogueadoId != null)
        {
            string? LogueadoEmail = HttpContext.Session.GetString("LogueadoEmail");
            string? LogueadoRol = HttpContext.Session.GetString("LogueadoRol");
            ViewBag.msgBienvenida = "Bienvenido/a  " + LogueadoEmail +" "+ LogueadoRol;
        }
        else
        {
            ViewBag.msgBienvenida = "Debe Iniciar Sesion ";
        }
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    public IActionResult BorrarSesion()
    {
        HttpContext.Session.Clear();
        

        return RedirectToAction("Index", "Home");
    }

}

