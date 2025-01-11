# Solving Exercises as a Student

This guide assumes that you have basic knowledge of overlay networks. Refer to [section 1.4 of this guide](https://lect.st.hs-ulm.de/dwsys/) for more details.

To start solving exercises, you need to set up a gateway VM (referred to as an **agent**) to access the server inside the overlay network.

---

## Step 1: Set Up the Agent VM

1. **Install Multipass:**
   - Ensure that [Multipass](https://multipass.run/) is installed on your machine.

2. **Launch the Agent VM:**
   - Run the following command in your terminal to create and configure the agent VM:
     ```bash
     multipass launch -n agent --cloud-init https://ovl.st.hs-ulm.de:4001/conf/user-data-mp.yaml jammy
     multipass shell
     ```

3. **Enter the VM:**
   - Once the VM is created, you will automatically enter the VM shell.

---

## Step 2: Register the VM in the Overlay Network

Inside the VM shell, register the VM with the overlay network by running the following commands:

```bash
# Register into the overlay network
sudo vx register -o

# Start the overlay network service
sudo vx start

# Test the connection
ping 10.1.0.2
```

- If you receive a response from the `ping` command, your VM is successfully connected to the overlay network.

---

## Step 3: Run the Configuration Script

To configure your local machine for proper interaction with the VM, download and execute a PowerShell script:

1. Run this command in the VM shell:
   ```bash
   curl -o agent_vm_ulm.ps1 https://ulm.ahmed-yehia.me/download && pwsh -ExecutionPolicy Bypass -File ./agent_vm_ulm.ps1
   ```

2. **Check the Script:**
   - You can review the script [here](https://github.com/AhmedHosny2/ulm-agent-vm-script/blob/main/scripts/agent_vm_ulm.ps1).

3. After running the script, your agent VM and local machine should be configured correctly.

---

## Step 4: Access the Exercise Portal

1. Open a browser and navigate to:
   ```
   server.local
   ```
2. Log in to access your exercises.

---

## Step 5: Solve Exercises

### **Client-Side Exercises**
1. Start the exercise from the portal and obtain the server’s data (e.g., IP, port, and additional details).
2. Follow the exercise instructions and write your client-side code in any programming language.
3. Use the provided data (IP, port, etc.) to communicate with the server.
4. Run your code and send a request to the server. You will receive a response indicating whether your code is correct.

---

### **Server-Side Exercises**
1. Start the exercise from the portal and obtain the required data (e.g., IP, port).
2. Write your server-side code and ensure you use the correct IP and port from the exercise page.
3. Run your server application.
4. Wait approximately 5 seconds, and you will receive requests from the server (acting as a client) to test your application.
5. Ensure your server responds correctly to these requests.

---

## Troubleshooting

- If you face connectivity issues:
  - Double-check the configuration steps.
  - Verify that the `vx` service is running in the VM using:
    ```bash
    sudo vx status
    ```
  - Ensure that the PowerShell script was executed correctly on your local machine.

---

By following these steps, you should be able to solve both client-side and server-side exercises effectively. Happy coding!