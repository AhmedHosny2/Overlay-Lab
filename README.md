
# Overlay Lab  

![High-Level System Architecture](https://github.com/user-attachments/assets/7cc186c5-feec-4022-bf1a-f049cead6004)  

A platform for isolated programming exercises using **Docker** containers and a secure **VXLAN** overlay network.  

---

## Table of Contents  
1. [Project Overview](#project-overview)  
2. [Installation Guide](#installation-guide)  
3. [Usage Instructions](#usage-instructions)  
4. [Key Features](#key-features)  
5. [Configuration](#configuration)  
6. [Contributing Guidelines](#contributing-guidelines)  
7. [Visual Enhancements](#visual-enhancements)
8. [Additional Resources](#Additional-Resources)

---

## Project Overview  
Overlay Lab provides an environment where students complete programming exercises in isolated Docker containers connected via a secure VXLAN overlay network.  

**Core Goals**:  
- **Isolation**: Containerized exercises prevent interference between users  
- **Security**: VXLAN network encrypts inter-container communication  
- **Flexibility**: JSON-configurable exercises for client/server challenges  

---

## Installation Guide  

### Prerequisites  
- [Docker](https://www.docker.com/get-started)  
- [Multipass](https://multipass.run/)  
- [NGINX](https://www.nginx.com/)  

### Setup Steps  
1. Clone the repository:  
   ```bash
   git clone https://github.com/AhmedHosny2/testbed-distributed-system.git
   cd projectname
   ```  

2. Install dependencies (Ubuntu example):  
   ```bash
   sudo apt update && sudo apt install docker.io multipass nginx
   ```  

3. Launch the agent VM:  
   ```bash
   multipass launch -n agent --cloud-init https://ovl.st.hs-ulm.de:4001/conf/user-data-mp.yaml jammy
   multipass shell
   ```  

---

## Usage Instructions  

1. **Register VM in Overlay Network**:  
   ```bash
   sudo vx register -o  # Register VM
   sudo vx start        # Start overlay service
   ping 10.1.0.2        # Test connectivity
   ```  

2. **Configure NGINX**:  
   ```bash
   curl -o init_nginx.sh https://ulm.ahmed-yehia.me/init_nginx && chmod +x init_nginx.sh && sudo ./init_nginx.sh
   curl -o update_srv_ip.sh https://ulm.ahmed-yehia.me/update_srv_ip && chmod +x update_srv_ip.sh && sudo ./update_srv_ip.sh
   ```  

3. **Finalize Setup**:  
   ```bash
   curl -o agent_vm_ulm.ps1 https://ulm.ahmed-yehia.me/download && pwsh -ExecutionPolicy Bypass -File ./agent_vm_ulm.ps1
   ```  

4. Access the portal at:  
   ```
   http://server.local
   ```  

---

## Key Features  
- **Container Isolation**: Dedicated Docker containers per exercise  
- **VXLAN Security**: Encrypted overlay network layer  
- **NGINX Proxy**: IP preservation, load balancing, and DNS resolution  
- **JSON Configuration**: Define exercises with custom parameters  

---

## Configuration  
Example JSON exercise definition:  
```json
{
    "ExerciseName": "grpc-app",
    "ExerciseReqConnectionType": "grpc",
    "ExerciseTile": "gRPC Adventure",
    "ExerciseDescription": "Dive into the world of gRPC! Build a client that connects to a gRPC server and interacts with it to create, fetch, list, and delete users.",
    "ExerciseDifficulty": "2",
    "DockerImage": "ahmedyh1/grcp_server",
    "port": "5015",
    "DisplayFields": [
      "Name",
      "State.Status",
      "Config.Hostname",
      "NetworkSettings.Networks.bridge.NetworkID"
    ],
    "ClientSide": false,
    "Variables": {
      "host": "",
      "exposedPort": ""
    }
}
```  

---

## Contributing Guidelines  
1. Fork the repository  
2. Create a feature branch:  
   ```bash
   git checkout -b feature/your-feature
   ```  
3. Follow existing code style  
4. Submit a PR with detailed description  

---

## Visual Enhancements  

### Client-Side Exercise Example

### Portal Interface  
Homepage showing available exercises with difficulty levels and descriptions:  

![HOMe page](https://github.com/user-attachments/assets/70c06d5c-3b97-4534-88fc-d0eed6449bd8)

---

**1. Exercise Overview**  
Students select client-side exercises from the portal. The system provides server container details (IP/port) for connection.  
![Client Exercise Interface](https://github.com/user-attachments/assets/dd73e656-1d3e-4f82-a620-9db1c79d1cf6)  

**2. Container Details**  
Real-time container networking information including IP address (`10.1.0.2`), exposed port (`5015`), and status:  
![Container Network Details](https://github.com/user-attachments/assets/d1e159b2-b11b-47e8-8c44-f2b0bf3b16d0)  

**3. Code Implementation**  
Students write client code to interact with the server container, with immediate feedback shown:  
![Client Code and Response](https://github.com/user-attachments/assets/ede43f22-2337-4cc6-993e-78f2c23700fd)  

---

### Server-Side Exercise Example

**1. Exercise Setup**  
Students launch server-side exercises and receive endpoint details (`10.1.0.3:8080`) to implement server logic:  
![Server Exercise Interface](https://github.com/user-attachments/assets/3f5ff2ab-84c4-4313-871b-02d6aa0ea10d)  

**2. Container Monitoring**  
Live view of client containers sending test requests to student servers:  
![Client Container Monitor](https://github.com/user-attachments/assets/47082e9b-bf14-4d48-9c3a-80904ee3eac3)  

**3. Server Implementation**  
Students write server code and see validation results from test containers:  
![Server Code Validation](https://github.com/user-attachments/assets/beb774b1-2f21-479c-b5ce-2af3d84937a0)  

---
## Additional Resources

This section contains links to supporting documentation and repositories that complement the **Educational Programming Platform**.

---

### 📄 Documentation
1. **Admin Setup**:  
   - [Admin: How to Deploy the Server](./docs/admin_deploy_server.md)  
   - [Admin: How to Create an Exercise](./docs/admin_create_exercise.md)  
2. **Student Workflow**:  
   - [Student: How to Solve an Exercise](./docs/student_solve_exercise.md)  

---

### 🛠️ Supporting Repositories
1. **Gateway VM Scripts**:  
   - Repository: [gateway-vm-scripts](https://github.com/AhmedHosny2/gateway-vm-scripts)  
   - Contains scripts to initialize NGINX and launch the VM gateway into the VXLAN overlay network.  

2. **Docker Images for Exercises**:  
   - Repository: [Docker-Images-For-Exercise](https://github.com/AhmedHosny2/Docker-Images-For-Exercise)  
   - Includes pre-built Docker images and examples of student solutions for exercises.  

---

### 🧩 Example Solutions
- Explore example solutions and Docker configurations in the [Overlay-Lab Docs](https://github.com/AhmedHosny2/Overlay-Lab/tree/main/docs).  

---
