using Microsoft.AspNetCore.Mvc;
using Portal.DeploymentService.Interface;

namespace Portal.DeploymentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ClientEvaluationController : ControllerBase
    {
        private readonly IDeploymentService _deploymentService;

        public ClientEvaluationController(IDeploymentService deploymentService)
        {
            _deploymentService = deploymentService;
        }

        // GET: api/ClientEvaluation/{uid}/{container_id}
        [HttpGet("{uid}/{container_id}")]
        public IActionResult ClientEvaluation(string uid, string container_id)
        {
            try
            {
                Console.WriteLine("Client evaluation for uid: {0} and container id: {1}", uid, container_id);
                _deploymentService.ClientExercisePassed(uid, container_id);
                Console.WriteLine("Client exercise passed successfully.");
                return Ok(new { Message = "Client exercise passed successfully.", Uid = uid, ContainerId = container_id });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}