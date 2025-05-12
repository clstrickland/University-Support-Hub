# University Support Hub

The application runs discreetly in the system tray and provides:
* Shortcuts to common IT-related applications (e.g., Intune Company Portal).
* Quick access to Windows Update and Intune Policy Sync.
* An integrated form to report issues directly to the help desk.

## Key Features

* **System Tray Integration:** Resides in the system tray for easy access without cluttering the desktop.
* **Quick Access Shortcuts:** Configurable shortcuts to launch essential applications like the Intune Company Portal and system settings like Windows Update.
* **Built-in Ticket Submission:** A simple form allowing users to provide:
    * Issue Title
    * Detailed Description
    * *Optional* Screenshot (with integrated screen capture tool)
* **System Information:** Automatically collects relevant system information (e.g., OS version, username, machine name) to send along with the ticket, aiding support personnel.
* **Modern Windows UI:** Built using modern Windows UI frameworks for a clean user experience.

## Documentation

You can find more information on this project in the [wiki](wiki).

## Technology Stack

* **Language:** C#
* **Framework:** .NET 8
* **UI:** WinUI 3
* **Packaging:** MSIX
* **Icons:** Font Awesome

## Contributing

Contributions are welcome! If you find a bug or have a feature request, please open an issue on the project's issue tracker. If you'd like to contribute code, please fork the repository and submit a pull request.

## Acknowledgements

* Inspired by the [macOS Support App](https://github.com/root3nl/SupportApp) developed by Root3.
* Uses Font Awesome for iconography.
