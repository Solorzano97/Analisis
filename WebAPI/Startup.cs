using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aplicacion.Bodega;
using Aplicacion.Clientes;
using Aplicacion.Contratos;
using Dominio;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Persistencia;
using Persistencia.DapperConexion;
using Persistencia.DapperConexion.Bodega;
using Seguridad;
using WebAPI.Middleware;

namespace WebAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddDbContext<InventarioOnlineContext>(opt =>{
                opt.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")); //conexion base de datos
            });


            services.AddOptions();
            services.Configure<ConexionConfiguracion>(Configuration.GetSection("DefaultConnection"));

            services.AddMediatR(typeof(Consulta.Manejador).Assembly); //configuracion mediatr
            
            services.AddControllers().AddFluentValidation(cfg => cfg.RegisterValidatorsFromAssemblyContaining<NuevoCliente>());
            var builder = services.AddIdentityCore<usuario>();//autenticacionde usuario
            var identityBuilder = new IdentityBuilder(builder.UserType , builder.Services);
            identityBuilder.AddEntityFrameworkStores<InventarioOnlineContext>();
            identityBuilder.AddSignInManager<SignInManager<usuario>>();
            services.TryAddSingleton<ISystemClock , SystemClock>();


            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("Mi palabra secreta")); //servicio que le pedira el token a los ususarios cuando quieran consultar tablas
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(opt => {
                opt.TokenValidationParameters = new TokenValidationParameters{
                    ValidateIssuerSigningKey  = true,
                    IssuerSigningKey = key ,
                    ValidateAudience = false,
                    ValidateIssuer = false 

                };
            });

            services.AddScoped<IJwtGenerador , JwtGenerador>();//seguridad
            services.AddScoped<IUsuarioSesion, UsuarioSesion>();

            //services.AddTransient<IFactoryConnection , FactoryConnection>(); //para trabajar con dapper
            //services.AddScoped<ibodegas , BodegaRepositorio>();


            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "WebAPI", Version = "v1" });
            });

            services.AddCors(options =>
        {
            options.AddDefaultPolicy(builder =>
            {
                builder.WithOrigins("http://localhost:3000")
                       .AllowAnyHeader()
                       .AllowAnyMethod();
            });
        });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            app.UseMiddleware<ManejadorErrorMidelware>();
            app.UseCors();
            if (env.IsDevelopment())
            {
                //app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "WebAPI v1"));
            }

            //app.UseHttpsRedirection();
            app.UseAuthentication();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            
        }
    }
}
