# Educational Programming Platform Documentation

This repository contains documentation for deploying, managing, and using the **Educational Programming Platform**, a system designed to streamline software development exercises through containerized environments and an overlay network.

---

## 📂 Available Documentation

### 1. [Admin: How to Deploy the Server](./admin_deploy_server.md)
- Step-by-step guide to set up the server VM.
- Covers installation, configuration, and deployment of the platform within the overlay network.

### 2. [Admin: How to Create an Exercise](./admin_create_exercise.md)
- Instructions for defining and managing exercises.
- Explains the JSON-based configuration system for modular exercise management.

### 3. [Student: How to Solve an Exercise](./student_solve_exercise.md)
- A walkthrough for students on interacting with exercises.
- Covers steps for both client-side and server-side exercises.

---

## 🔧 Key Features of the Platform
- **Modular Configuration**: Easily add or modify exercises using JSON files.
- **Dynamic Docker Containers**: Containers automatically spin up for exercises and manage lifecycle, IPs, and ports.
- **Secure Networking**: Built on a VXLAN overlay network with NGINX as a reverse proxy.
- **Two Exercise Types**:
  - Server-Side: Students implement servers to handle requests.
  - Client-Side: Students write clients to interact with provided servers.

---

## 🎯 Get Started

Start with the appropriate guide:
1. **Admins**: Use the [server deployment](./admin_deploy_server.md) and [exercise creation](./admin_create_exercise.md) guides.
2. **Students**: Follow the [exercise-solving guide](./student_solve_exercise.md) to begin your exercises.

---

This repository is part of a project aimed at enhancing hands-on learning for software development students. For more information, refer to the respective guides above.
