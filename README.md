# G.E.A.R Outdoor Navigation App

## 1. About G.E.A.R Outdoor Navigation App

**G.E.A.R (Google Enhanced Augmented Reality) Outdoor Navigation App** is a mobile AR application developed for navigating open environments like college campuses. It overlays directional arrows on the real-world view to guide users from one location to another using AR technology powered by Unity and ARCore.

This app is designed to improve navigation for new visitors and students by providing visual AR-based arrows and customizable models that show the route clearly. It combines GPS and VPS (Visual Positioning System) to deliver accurate navigation guidance.

<p align="center">
  <img src="assets/outdoor.gif" alt="Indoor Gear" width="30%" />
</p>


### Key Highlights

- AR arrows indicate real-time direction to destinations.
- Users can select custom arrow models.
- Navigation begins after selecting **FROM** and **TO** points.
- Designed initially for **MSEC campus**, covering 30 outdoor routes.
- No internet required post-installation (offline AR experience).

> The system is capable of plotting models globally using latitude, longitude, and altitude values, making it adaptable beyond MSEC.

---

## 2. Hardware Used

The following hardware components are necessary for using the app:

- **Smartphone with ARCore support**
- **Built-in GPS module**
- **Camera & motion sensors**
- **QR Code Scanner App** (to install from Google Drive)

> 📲 ARCore-compatible devices are essential for smooth AR rendering and motion tracking.

---

## 3. Software Used

| Software         | Role |
|------------------|------|
| Unity Engine     | To create AR and UI experience |
| ARCore SDK       | Google's AR toolkit for Android |
| AR Foundation    | Multi-platform AR abstraction |
| AR+GPS Location  | GPS-based real-world 3D object positioning |
| Blender          | For 3D model creation (e.g., arrows, blocks) |
| Inkscape         | For designing 2D arrow graphics for 3D conversion |

### Development Flow

- 2D arrow vectors designed in **Inkscape**  
- Converted to 3D in **Blender**  
- Imported into **Unity** for rendering via **ARCore + GPS plugin**

---

## 4. Applications

- 🧭 **Outdoor Campus Navigation**  
  Users can find their way between various blocks on campus (e.g., Main Gate to Hostel, Civil Block, etc.).

- 🧳 **Visitor Assistance**  
  Helps first-time visitors navigate without asking for directions.

- 🌍 **Global Deployment**  
  Can be adapted to any institution, city campus, or public area by modifying GPS data.

- 🏫 **Educational Institutions**  
  Especially useful in universities and large campuses for real-time AR-based routing.

### Example Implementations

- 🔹 6 MSEC campus blocks covered  
- 🔹 30 total routes programmed  
- 🔹 Example Route: **Main Gate → Main Block (Route 16, 61)**

> ✅ Users simply select "NAVIGATE FROM", "NAVIGATE TO", and an arrow model, then follow the AR arrows on-screen.

---

## 🔮 Future Developments

A future wearable enhancement called **G.E.A.R Lens** is planned, featuring:

- AR-based Navigation
- Object Recognition
- Human Face Detection and Matching
- AI-based Virtual Assistant

---

## 📌 Conclusion

This project provides an intuitive AR-based navigation solution using mobile devices. With features like directional arrows, contextual 3D labeling, and easy deployment via QR code, G.E.A.R enhances wayfinding in both outdoor and indoor environments.

> This project can be scaled to various domains like airports, malls, museums, or campuses, reducing confusion and improving user experience.