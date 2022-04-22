# OTPP Interiew Case Challenge 2022

This solution requires [.Net Core SDK 3.1](https://dotnet.microsoft.com/en-us/download/dotnet/3.1).

# Software Components

## PortfolioRisk.Core

This library project provides all shared code logic for the other two programs.

## Risk Terminal

A CLI (Command-Line Interface) program that provides general purpose risk analysis using a command line interface.

Command format: `RiskTerminal -a <Total Allocation> -t <Ticker Names> -w <Ticker Weights>` 
Example: `RiskTerminal -a 2,000,000 -t <Ticker Names> -w <Ticker Weights>`

Use command `RiskTerminal sample` to run the sample data as in Question 1 of *interview questions.pdf*.

## RiskBuilderWebApp

An interactive single-page Blazor web app that allows construct and analysis of portfolio risk through web interface.

This can be further improved with more sophisticated background worker management when risk data gets large.

## Data Source (Folder)

This folder contains some sample data, it's not used by code.