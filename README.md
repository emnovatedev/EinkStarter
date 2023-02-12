# Introduction 
TODO: Give a short introduction of your project. Let this section explain the objectives or the motivation behind this project. 

# Purpose
This document describes the process for converting and transferring images to the E-ink screen, using a reference mobile application and development board.

# Overview
Upon unboxing, the e-ink screen must be attached to the development board, and power must be applied using one of the supported methods.
Before images can be transferred to the screen, they must undergo a transformation.  First, their color spaces are transformed using the nColorConvert utility.  Later, the resulting images are processed using the GalleryPallette2Bin utility, to prepare them for the digital transfer process using the mobile application software.   
After conversion, the images are loaded at runtime by the mobile application, allowing for them to be transferred to the device when the appropriate commands are selected by the user.

# Hardware 
The packaging includes one development board, along with a 4-inch E-ink screen.  Upon unboxing, the screen must be attached to the development board as depicted in Figure 1 below.  After the screen is attached, power can be applied to test a proper connection.

# Documentation 

Access the Application Interface Specification in at this location in the repo https://github.com/emnovatedev/EinkStarter/blob/main/docs/Application%20Interface%20Document.docx.

Access the MS word version of the implementation guide here, https://docs.google.com/document/d/1gMHXYD3VF8Z6WygcCxgGo_G8ezXUfY-I/edit?usp=sharing&ouid=106371756306007537362&rtpof=true&sd=true.

Access and online version of the implementation guide here as a google doc, https://docs.google.com/document/d/1gMHXYD3VF8Z6WygcCxgGo_G8ezXUfY-I/edit?usp=sharing&ouid=106371756306007537362&rtpof=true&sd=true.
A step by step video tutorial is locate here, https://youtu.be/YYvBMbdJ7vo
