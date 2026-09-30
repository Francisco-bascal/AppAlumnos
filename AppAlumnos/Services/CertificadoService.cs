using AppAlumnos.DTOs;
using AppAlumnos.Exceptions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Collections.Generic;

namespace AppAlumnos.Services
{
    public class CertificadoService
    {
        private const string Institucion = "Instituto Superior de Formación Técnica";
        private const string DireccionInstitucion = "Av. de los Estudiantes 1234 - Ciudad";

        private readonly ILogger<CertificadoService> _logger;

        public CertificadoService(ILogger<CertificadoService> logger)
        {
            _logger = logger;
        }

        public byte[] GenerarCertificadoAlumnoRegular(string apellidoNombre, string dni, int anioLectivo)
        {
            return GenerarDocumento(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Black));

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text(txt =>
                        {
                            txt.Span(Institucion).Bold().FontSize(16);
                        });
                        col.Item().AlignCenter().Text(txt =>
                        {
                            txt.Span(DireccionInstitucion).FontSize(10).FontColor(Colors.Grey.Darken1);
                        });
                        col.Item().AlignCenter().Text(txt =>
                        {
                            txt.Span($"Ciclo Lectivo {anioLectivo}").FontSize(10).FontColor(Colors.Grey.Darken1);
                        });
                        col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingTop(30).Column(col =>
                    {
                        col.Item().AlignCenter().Text(txt =>
                        {
                            txt.Span("CERTIFICADO DE ALUMNO REGULAR").Bold().FontSize(15);
                        });

                        col.Item().PaddingVertical(24).Text(txt =>
                        {
                            txt.Span("Se certifica que ");
                            txt.Span(apellidoNombre).Bold();
                            txt.Span($", DNI {dni}, se encuentra inscripto/a como alumno/a REGULAR del establecimiento durante el Ciclo Lectivo {anioLectivo}, según consta en el sistema de gestión académica del Instituto.");
                        });

                        col.Item().PaddingTop(20).AlignCenter().Text(txt =>
                        {
                            txt.Span("Se expide el presente certificado a solicitud de la parte interesada ");
                            txt.Span("y para los fines que estime conveniente.");
                        });
                    });

                    page.Footer().PaddingTop(20).Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        col.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Text(FechaActual());
                            row.RelativeItem().AlignCenter().Text(txt =>
                            {
                                txt.Span("AUTORIDAD COMPETENTE").Bold().FontSize(10);
                            });
                            row.RelativeItem().AlignRight().Text(txt =>
                            {
                                txt.Span("Recepción y Mesa de Entradas").FontSize(10);
                            });
                        });
                    });
                });
            }, "certificado-alumno-regular");
        }

        public byte[] GenerarCertificadoMateriasAprobadas(
            string apellidoNombre,
            string dni,
            IReadOnlyCollection<MateriaAprobadaDto> materias)
        {
            return GenerarDocumento(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Black));

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text(txt =>
                        {
                            txt.Span(Institucion).Bold().FontSize(16);
                        });
                        col.Item().AlignCenter().Text(txt =>
                        {
                            txt.Span(DireccionInstitucion).FontSize(10).FontColor(Colors.Grey.Darken1);
                        });
                        col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingTop(30).Column(col =>
                    {
                        col.Item().AlignCenter().Text(txt =>
                        {
                            txt.Span("CERTIFICADO DE MATERIAS APROBADAS").Bold().FontSize(15);
                        });

                        col.Item().PaddingVertical(16).Text(txt =>
                        {
                            txt.Span("Se certifica que ");
                            txt.Span(apellidoNombre).Bold();
                            txt.Span($", DNI {dni}, ha aprobado las siguientes materias de la carrera, con la calificación y año lectivo que se detallan:");
                        });

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(90);
                                columns.RelativeColumn();
                                columns.ConstantColumn(80);
                            });

                            table.Cell().Element(CeldaCabecera).AlignCenter().Text("Año Lectivo");
                            table.Cell().Element(CeldaCabecera).Text("Materia");
                            table.Cell().Element(CeldaCabecera).AlignCenter().Text("Nota");

                            var filaCentral = 0;
                            foreach (var materia in materias)
                            {
                                var estilo = (filaCentral % 2 == 0)
                                    ? (Func<IContainer, IContainer>)CeldaFila
                                    : CeldaFilaZebra;
                                table.Cell().Element(estilo).Text(materia.Anio.ToString());
                                table.Cell().Element(estilo).Text(materia.Nombre);
                                table.Cell().Element(estilo).AlignCenter().Text(materia.Nota.ToString("0.#"));
                                filaCentral++;
                            }
                        });

                        col.Item().PaddingTop(16).Text(txt =>
                        {
                            txt.Span($"Conste que el/los certificados de aprobación suman {materias.Count} materia(s) a fines de constancia de avance académico.");
                        });
                    });

                    page.Footer().PaddingTop(20).Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        col.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Text(FechaActual());
                            row.RelativeItem().AlignCenter().Text(txt =>
                            {
                                txt.Span("AUTORIDAD COMPETENTE").Bold().FontSize(10);
                            });
                            row.RelativeItem().AlignRight().Text(txt =>
                            {
                                txt.Span("Recepción y Mesa de Entradas").FontSize(10);
                            });
                        });
                    });
                });
            }, "certificado-materias-aprobadas");
        }

        public byte[] GenerarListadoInscriptos(
            string materiaNombre,
            IReadOnlyCollection<InscriptoNotasDto> inscriptos)
        {
            // Los ciclos se derivan de las filas para que el encabezado nunca pueda
            // contradecir el contenido de la tabla.
            var ciclos = inscriptos
                .Select(i => i.AnioLectivo)
                .Distinct()
                .OrderBy(a => a)
                .ToList();
            var detalleCiclos = ciclos.Count == 0
                ? string.Empty
                : $" - Ciclos Lectivos: {string.Join(", ", ciclos)}";

            return GenerarDocumento(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Black));

                    page.Header().Column(col =>
                    {
                        col.Item().AlignCenter().Text(txt =>
                        {
                            txt.Span(Institucion).Bold().FontSize(16);
                        });
                        col.Item().AlignCenter().Text(txt =>
                        {
                            txt.Span(DireccionInstitucion).FontSize(10).FontColor(Colors.Grey.Darken1);
                        });
                        col.Item().AlignCenter().Text(txt =>
                        {
                            txt.Span($"LISTADO DE ALUMNOS INSCRIPTOS{detalleCiclos}").FontSize(10).FontColor(Colors.Grey.Darken1);
                        });
                        col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                    });

                    page.Content().PaddingTop(24).Column(col =>
                    {
                        col.Item().AlignCenter().Text(txt =>
                        {
                            txt.Span($"Materia: {materiaNombre}").Bold().FontSize(13);
                        });

                        col.Item().PaddingTop(14).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(34);
                                columns.ConstantColumn(78);
                                columns.RelativeColumn();
                                columns.ConstantColumn(60);
                                columns.ConstantColumn(100);
                            });

                            table.Cell().Element(CeldaCabecera).AlignCenter().Text("N°");
                            table.Cell().Element(CeldaCabecera).AlignCenter().Text("Año Lectivo");
                            table.Cell().Element(CeldaCabecera).Text("Apellido y Nombre");
                            table.Cell().Element(CeldaCabecera).AlignCenter().Text("Nota");
                            table.Cell().Element(CeldaCabecera).AlignCenter().Text("Estado");

                            var filaCentral = 0;
                            foreach (var alumno in inscriptos)
                            {
                                var estilo = (filaCentral % 2 == 0)
                                    ? (Func<IContainer, IContainer>)CeldaFila
                                    : CeldaFilaZebra;
                                table.Cell().Element(estilo).AlignCenter().Text((filaCentral + 1).ToString());
                                table.Cell().Element(estilo).AlignCenter().Text(alumno.AnioLectivo.ToString());
                                table.Cell().Element(estilo).Text(alumno.ApellidoNombre);
                                table.Cell().Element(estilo).AlignCenter()
                                    .Text(alumno.Nota.HasValue ? alumno.Nota.Value.ToString("0.#") : "-");
                                table.Cell().Element(estilo).AlignCenter().Text(alumno.Estado);
                                filaCentral++;
                            }
                        });

                        col.Item().PaddingTop(14).Text(txt =>
                        {
                            txt.Span($"Cantidad total de alumnos: {inscriptos.Count}");
                        });
                    });

                    page.Footer().PaddingTop(20).Column(col =>
                    {
                        col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten1);
                        col.Item().PaddingTop(8).Row(row =>
                        {
                            row.RelativeItem().Text(FechaActual());
                            row.RelativeItem().AlignCenter().Text(txt =>
                            {
                                txt.Span("DOCENTE RESPONSABLE").Bold().FontSize(10);
                            });
                            row.RelativeItem().AlignRight().Text(txt =>
                            {
                                txt.Span($"Total de alumnos: {inscriptos.Count}").FontSize(10);
                            });
                        });
                    });
                });
            }, "listado-inscriptos");
        }

        private byte[] GenerarDocumento(Action<IDocumentContainer> contenido, string nombreDocumento)
        {
            try
            {
                return Document.Create(contenido).GeneratePdf();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "No se pudo generar el documento {Documento}.", nombreDocumento);
                throw new FalloGeneracionDocumentoException("No se pudo generar el documento.", ex);
            }
        }

        private static IContainer CeldaCabecera(IContainer container)
        {
            return container.Background(Colors.Blue.Darken4)
                .PaddingVertical(6)
                .PaddingHorizontal(8)
                .DefaultTextStyle(x => x.FontSize(10).FontColor(Colors.White).Bold());
        }

        private static IContainer CeldaFila(IContainer container)
        {
            return container.Background(Colors.White)
                .PaddingVertical(5)
                .PaddingHorizontal(8)
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten2);
        }

        private static IContainer CeldaFilaZebra(IContainer container)
        {
            return container.Background(Colors.Grey.Lighten4)
                .PaddingVertical(5)
                .PaddingHorizontal(8)
                .BorderBottom(0.5f)
                .BorderColor(Colors.Grey.Lighten2);
        }

        private static string FechaActual()
        {
            return DateTime.Now.ToString("dd 'de' MMMM 'de' yyyy").ToLowerInvariant();
        }
    }
}