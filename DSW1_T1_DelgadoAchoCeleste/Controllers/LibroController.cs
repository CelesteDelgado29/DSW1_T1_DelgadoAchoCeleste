using DSW1_T1_DelgadoAchoCeleste.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace DSW1_T1_DelgadoAchoCeleste.Controllers
{
    public class LibroController : Controller
    {
        private readonly string cadena;

        public LibroController(IConfiguration config)
        {
            cadena = config["ConnectionStrings:cn"]!;
        }

        IEnumerable<Libro> ListarLibros()
        {
            var lista = new List<Libro>();
            using (var cn = new SqlConnection(cadena))
            {
                cn.Open();
                var cmd = new SqlCommand("usp_ListarLibros", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                using var dr = cmd.ExecuteReader();
                while (dr.Read()) lista.Add(Leer(dr));
            }
            return lista;
        }

        IEnumerable<Libro> ListarLibrosPorAutor(string autor)
        {
            var lista = new List<Libro>();
            using (var cn = new SqlConnection(cadena))
            {
                cn.Open();
                var cmd = new SqlCommand("usp_ListarLibrosPorAutor", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Autor", autor);
                using var dr = cmd.ExecuteReader();
                while (dr.Read()) lista.Add(Leer(dr));
            }
            return lista;
        }

        Libro Leer(SqlDataReader dr)
        {
            return new Libro
            {
                Id = dr.GetInt32(0),
                NombreLibro = dr.GetString(1),
                Autor = dr.GetString(2),
                NombreEditorial = dr.GetString(3),
                ISDN = dr.GetString(4)
            };
        }

        Libro? Buscar(int id)
        {
            return ListarLibros().FirstOrDefault(x => x.Id == id);
        }

        int Ejecutar(string procedimiento, Libro l, bool conId)
        {
            using var cn = new SqlConnection(cadena);
            cn.Open();
            var cmd = new SqlCommand(procedimiento, cn);
            cmd.CommandType = CommandType.StoredProcedure;
            if (conId) cmd.Parameters.AddWithValue("@Id", l.Id);
            cmd.Parameters.AddWithValue("@NombreLibro", l.NombreLibro);
            cmd.Parameters.AddWithValue("@Autor", l.Autor);
            cmd.Parameters.AddWithValue("@NombreEditorial", l.NombreEditorial);
            cmd.Parameters.AddWithValue("@ISDN", l.ISDN);
            return cmd.ExecuteNonQuery();
        }

        public IActionResult Index()
        {
            return View(ListarLibros());
        }

        public IActionResult Crear()
        {
            return View(new Libro());
        }

        [HttpPost]
        public IActionResult Crear(Libro l)
        {
            if (!ModelState.IsValid) return View(l);
            try
            {
                Ejecutar("usp_RegistrarLibro", l, false);
                ViewBag.Mensaje = "Libro registrado correctamente";
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = "No se pudo registrar: " + ex.Message;
            }
            return View("Index", ListarLibros());
        }

        public IActionResult Editar(int id)
        {
            var l = Buscar(id);
            if (l == null) return RedirectToAction("Index");
            return View(l);
        }

        [HttpPost]
        public IActionResult Editar(Libro l)
        {
            if (!ModelState.IsValid) return View(l);
            try
            {
                Ejecutar("usp_ActualizarLibro", l, true);
                ViewBag.Mensaje = "Libro actualizado correctamente";
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = "No se pudo actualizar: " + ex.Message;
            }
            return View("Index", ListarLibros());
        }

        [HttpPost]
        public IActionResult Eliminar(int id)
        {
            try
            {
                using var cn = new SqlConnection(cadena);
                cn.Open();
                var cmd = new SqlCommand("usp_EliminarLibro", cn);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", id);
                cmd.ExecuteNonQuery();
                ViewBag.Mensaje = "Libro eliminado correctamente";
            }
            catch (Exception ex)
            {
                ViewBag.Mensaje = "No se pudo eliminar: " + ex.Message;
            }
            return View("Index", ListarLibros());
        }

        public IActionResult PorAutor(string? autor)
        {
            var lista = string.IsNullOrWhiteSpace(autor)
                ? new List<Libro>()
                : ListarLibrosPorAutor(autor).ToList();
            ViewBag.Autor = autor;
            if (!string.IsNullOrWhiteSpace(autor) && lista.Count == 0)
                ViewBag.Mensaje = "No se encontraron libros de ese autor";
            return View(lista);
        }
    }
}