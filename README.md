# 🛡️ Klient_init-tank_Unity
### *The Legacy Lives On: Reconstructing the Golden Era of Tanki Online*

![Unity](https://img.shields.io/badge/Made%20with-Unity%202022.3-black.svg?style=for-the-badge&logo=unity)
![C#](https://img.shields.io/badge/Language-C%23-purple.svg?style=for-the-badge&logo=c-sharp)
![Platform](https://img.shields.io/badge/Platform-Windows-0078D4.svg?style=for-the-badge&logo=windows)

---

## 🌟 The Vision
This project is a high-fidelity reconstruction of the legendary **Tanki Online** Flash client (2012-2015 era). We aren't just building a game; we are preserving a piece of gaming history. By migrating the original ActionScript 3 logic to **Unity & C#**, we bring modern stability and performance to the classic gameplay we all love.

---

## ✨ Key Highlights

### 🎨 Pixel-Perfect UI Reconstruction
*   **Legacy Skinning**: 1:1 recreation of the classic "Grey", "Green", and "Gold" UI button systems.
*   **Authentic Layouts**: Every pixel of the Lobby, Chat, and Entrance screen is measured against the original client.
*   **Dynamic UI Builders**: Custom Editor tools that assemble the interface using original Flash texture libraries.

### ⚙️ Engine & Architecture
*   **OSGi-Inspired Core**: A robust service-registry system that mirrors the original `alternativa.osgi` pattern for maximum modularity.
*   **Legacy Networking**: Native support for the **FlashTanki** socket protocol, including:
    *   Command parsing with `~dne` delimiters.
    *   Authentic AES and Shift-key encryption.
    *   Windows-1251 character encoding for server compatibility.

### 🚀 Modern Performance
*   Leveraging **Unity's URP/Built-in** rendering for smooth 60+ FPS gameplay.
*   Optimized asset management using **ScriptableObjects** and efficient memory layouts.

---

## 📊 Current Roadmap

| Feature | Status | Description |
| :--- | :---: | :--- |
| **Project Core** | ✅ | Environment setup, Git integration, and Version control. |
| **Lobby UI** | 🏗️ | Entrance, Top Panel, and Tabbed Communication system. |
| **Networking** | 🛠️ | Socket implementation and protocol handshake. |
| **Battle System** | 📅 | *Planned:* Tank physics and classic weapon mechanics. |
| **Garage** | 📅 | *Planned:* Equipment management and 3D preview. |

---

## 🛠️ How to Launch
1. Ensure you have **Unity 2022.3.62f3** installed.
2. Use the provided `launch_unity.bat` for a guaranteed version-safe startup.
3. Dive into the source and help us rebuild the legend!

---

## 🇷🇺 На русском
Это амбициозный проект по воссозданию "тех самых" Танков Онлайн на движке Unity. Мы бережно переносим каждый элемент интерфейса, каждую строчку сетевой логики из оригинального AS3-кода, чтобы дать классике вторую жизнь в современном исполнении.

---

## ⚖️ Legal Disclaimer
*This repository is for educational and archival purposes. All original assets, trademarks, and brand names are the property of Alternativa Games. This is a community-driven preservation project.*

---
**Maintained with ❤️ by [radiomonter](https://github.com/radiomonter)**
