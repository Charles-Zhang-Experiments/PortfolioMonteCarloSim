# OTPP Interiew Case Challenge 2022

This repository contains a complete solution for portfolio risk simulation based on samples from historical returns.

It's an implementation of the *Programming Project* for the **Investment Analyst** position at *Total Fund Risk* for OTPP in 2022 April.

The original programming project contains three parts (three questions), a summary of which is provided below:

1. Question 1: Implement a solution for simulating portfolio risk from a weighted selection of assets (see **Sample Portfolio** section at the end of this page), by extracting 4 quarters of historical data and perform 5000 simulations as projection for future outcome;
2. Question 2: Make comments on the legitimacy of the methodology and implementation results;
3. Question 3: Provide a web GUI interface for Question 1.

## Overview

There are two complications in the overall solution (e.g. for Question 1):

1. **Data downloading and preprocessing**: For manual operations one can download CSV files from the web and manually clean up the data for missing entries and mis-matching dates, but for generic general-purpose API-based data source, the program must be smart enough to automatically handle errors in data. This part of code is mostly done inside `PortfolioAnalyzer` class.
2. **Data simulation and processing**: When proper data is fetched and ready for processing, the actual simulation is quite trivial; This is done with the help of classes inside `Algorithm` namespace.

## How to Run

This solution requires [.Net Core SDK 3.1](https://dotnet.microsoft.com/en-us/download/dotnet/3.1).

For compiling and run:

1. Open *OTPPInterview2022.sln* in Visual Studio or Rider;
2. Set *RiskTerminal* or *PortfolioBuilderWebApp* as starting project;
3. Press *F5* to run, It should take less than a minute to execute after compilation is done; 
4. The outcome will be shown inside the CLI console output and inside a pop-up window.

For standalone use:

* Download and unzip [latest build for windows]();
* Execute either RiskTerminal.exe or PortfolioBuilderWebApp.exe;
* For command line parameters, see section on *Risk Terminal* below.

For quick online test:

* Go to https://totalimagine.com/risk, it provides the examples in Question 1 as preset

# Software Components

The whole solution is written entirely in C# (with some JavaScript and HTML), and is divided into three parts: 

1. (.Net Core Class Library) PortfolioRisk.Core: Main solution logic;
2. (.Net Core Console Program) Risk Terminal: CLI entrypoint for the solution;
3. (.Net Core Blazor Web Assembly Application) Portfolio Builder Web App: Web-based interface for the solution;
4. (.Net WPF Application) ChartViewer: A small utility program providing line chart visualization for simulation outcome.

## PortfolioRisk.Core

This library project provides all shared code logic for the other two programs.

## Risk Terminal

A CLI (Command-Line Interface) program that provides general purpose risk analysis using a command line interface.

Command format: `RiskTerminal -t <Total Allocation> -a <Assets> -w <Asset Weights> -f <Factors> -s <Start Date> -e <End Date>` 
Example: `RiskTerminal -t 2,000,000 -a SPY XIU -w 1 1 -f SPY XIU USD/CAD -s 2017-01-01 -e 2021-12-31`

Use command `RiskTerminal sample` or simply run `RiskTerminal` without any command-line arguments to run the sample data as in Question 1.

## Portfolio Builder Web App

An interactive single-page Blazor web app that allows construct and analysis of portfolio risk through web interface.

This can be further improved with more sophisticated background worker management when risk data gets large.

## Offline Data Sources (Folder)

The implementation provides automatic ticker historical data fetching capabilities from Yahoo Finance, notice due to the closed-source nature of Yahoo Finance API (official Yahoo Finance API was disabled in 2017), there may be issues with fetching data when unexpected formats are encountered - that's why an offline data source is provided, as described below.

The *PortfolioRisk.Core/OfflineSources* folder contains some sample data, it's used for the first question and provides offline access of data.

# Sample Portfolio (Question 1 & 2)

The sample portfolio assumes a $CAD2B portfolio with SPY and XIU weighted 1:1. Notce that the quotes for iShare (XIU) is in CAD while SPY is in USD. That's why we have a USD/CAD historical rate for currency conversion purpose.

Since both the offline data and Yahoo Finance API data can have missing dates, or otherwise contain mismatching numbers of rows - they need to be cleaned for alignment and contains matching number of rows before being used at later stages of simulation.

## Methodology

1. Data needs clean up, some are filled for missing data.