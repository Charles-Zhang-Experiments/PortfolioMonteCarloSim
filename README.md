# OTPP Interiew Case Challenge 2022

This solution requires [.Net Core SDK 3.1](https://dotnet.microsoft.com/en-us/download/dotnet/3.1).

## Overview

There are two complications in the overall solution (e.g. for Question 1):

1. Data downloading and preprocessing: For manual operations one can download CSV files from the web and manually clean up the data for missing entries and mis-matching dates, but for generic general-purpose API-based data source, the program must be smart enough to automatically handle errors in data. This part of code is mostly done inside `PortfolioAnalyzer` class.
2. Data simulation and processing: When proper data is fetched and ready for processing, the actual simulation is quite trivial; This is done with the help of classes inside `Algorithm` namespace.

## How to Run

For compiling and run:

1. Open *OTPPInterview2022.sln* in Visual Studio or Rider
2. Set *RiskTerminal* or *PortfolioBuilderWebApp* as starting project
3. Press *F5* to run

For quick online test:

* Go to https://totalimagine.com/risk, it provides the examples in Question 1 as preset

# Software Components

The whole solution is written entirely in C# (with some JavaScript and HTML), and is divided into three parts: 

1. (.Net Core Class Library) PortfolioRisk.Core
2. (.Net Core Console Program) Risk Terminal
3. (.Net Core Blazor Web Assembly Application) Portfolio Builder Web App

## PortfolioRisk.Core

This library project provides all shared code logic for the other two programs.

## Risk Terminal

A CLI (Command-Line Interface) program that provides general purpose risk analysis using a command line interface.

Command format: `RiskTerminal -t <Total Allocation> -a <Assets> -w <Asset Weights> -f <Factors> -s <Start Date> -e <End Date>` 
Example: `RiskTerminal -t 2,000,000 -a SPY XIU -w 1 1 -f SPY XIU USD/CAD`

Use command `RiskTerminal sample` or simply run `RiskTerminal` without any command-line arguments to run the sample data as in Question 1 of *interview questions.pdf*.

## Portfolio Builder Web App

An interactive single-page Blazor web app that allows construct and analysis of portfolio risk through web interface.

This can be further improved with more sophisticated background worker management when risk data gets large.

## Offline Data Sources (Folder)

This folder contains some sample data, it's used for the first question and offline access of some data.

Notice due to restrictions of Yahoo Finance API and since I can't post my personal access codes in public, there may be issues with fetching data.

Notce iShare (XIU) is in CAD while SPY is in USD.

Notice both the offline data and API data can have missing dates, or otherwise contain mismatching numbers of rows - they need to be cleaned for alignment and contains matching number of rows before using.

# Question 2

## Methodology

1. Data needs clean up, some are filled for missing data.