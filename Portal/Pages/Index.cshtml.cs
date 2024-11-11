using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Docker.DotNet;
using Docker.DotNet.Models;
using Portal.Models;
using System.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using Portal.DeploymentService.Interface;
using Portal.DeploymentService.Class;


namespace Portal.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private IDeploymentService _deploymentService = new Portal.DeploymentService.Class.DeploymentService();


      public IList<ServerInstance> Containers { get; set; } = new List<ServerInstance>();

    // create DeploymentService object

    // public IndexModel(ILogger<IndexModel> logger)
    // {
    //     _logger = logger;
    // }

   
    // list containers
    public async Task OnGetAsync()
    {
        Containers= await _deploymentService.ListContainers(_deploymentService.ConnectToDocker());
    }


}
