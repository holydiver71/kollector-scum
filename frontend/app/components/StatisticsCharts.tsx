"use client";
import React from 'react';
import {
  YearStatistic,
} from '../lib/api';

interface BarChartProps {
  data: { label: string; value: number; percentage?: number }[];
  title: string;
  maxBars?: number;
}

export function BarChart({ data, title, maxBars = 10 }: BarChartProps) {
  const displayData = data.slice(0, maxBars);
  const maxValue = Math.max(...displayData.map(d => d.value));

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <h3 className="text-lg font-semibold text-gray-900 mb-4">{title}</h3>
      <div className="space-y-3">
        {displayData.map((item, index) => (
          <div key={index}>
            <div className="flex items-center justify-between mb-1">
              <span className="text-sm font-medium text-gray-700">{item.label}</span>
              <span className="text-sm text-gray-600">
                {item.value} {item.percentage !== undefined && `(${item.percentage}%)`}
              </span>
            </div>
            <div className="w-full bg-gray-200 rounded-full h-2.5">
              <div
                className="bg-blue-600 h-2.5 rounded-full transition-all duration-500"
                style={{ width: `${(item.value / maxValue) * 100}%` }}
              ></div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

interface LineChartProps {
  data: YearStatistic[];
  title: string;
}

export function LineChart({ data, title }: LineChartProps) {
  if (data.length === 0) {
    return (
      <div className="bg-white rounded-lg border border-gray-200 p-6">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">{title}</h3>
        <p className="text-gray-500">No data available</p>
      </div>
    );
  }

  const maxValue = Math.max(...data.map(d => d.count));
  const minYear = Math.min(...data.map(d => d.year));
  const maxYear = Math.max(...data.map(d => d.year));

  // Create a simple bar chart visualization for years
  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <h3 className="text-lg font-semibold text-gray-900 mb-4">{title}</h3>
      <div className="flex items-end justify-between h-64 gap-1">
        {data.map((item, index) => {
          const height = (item.count / maxValue) * 100;
          return (
            <div key={index} className="flex-1 flex flex-col items-center group">
              <div className="relative w-full">
                {/* Tooltip */}
                <div className="absolute bottom-full left-1/2 transform -translate-x-1/2 mb-2 px-2 py-1 bg-gray-900 text-white text-xs rounded opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none z-10">
                  {item.year}: {item.count}
                </div>
                {/* Bar */}
                <div
                  className="w-full bg-blue-600 rounded-t hover:bg-blue-700 transition-colors cursor-pointer"
                  style={{ height: `${height}%`, minHeight: height > 0 ? '4px' : '0' }}
                ></div>
              </div>
              {/* Year label - show every 5th year or first/last */}
              {(index === 0 || index === data.length - 1 || item.year % 5 === 0) && (
                <span className="text-xs text-gray-600 mt-2 transform -rotate-45 origin-top-left">
                  {item.year}
                </span>
              )}
            </div>
          );
        })}
      </div>
      <div className="mt-6 text-sm text-gray-600 text-center">
        Years: {minYear} - {maxYear}
      </div>
    </div>
  );
}

interface StatCardProps {
  title: string;
  value: string | number;
  subtitle?: string;
  icon?: string;
  trend?: {
    value: number;
    isPositive: boolean;
  };
}

export function StatCard({ title, value, subtitle, icon, trend }: StatCardProps) {
  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6 hover:shadow-md transition-shadow">
      <div className="flex items-center justify-between">
        <div className="flex-1">
          <p className="text-sm font-medium text-gray-600">{title}</p>
          <p className="text-3xl font-bold text-gray-900 mt-2">{value}</p>
          {subtitle && <p className="text-sm text-gray-500 mt-1">{subtitle}</p>}
          {trend && (
            <div className={`flex items-center mt-2 text-sm ${trend.isPositive ? 'text-green-600' : 'text-red-600'}`}>
              <span>{trend.isPositive ? '↑' : '↓'}</span>
              <span className="ml-1">{Math.abs(trend.value)}%</span>
            </div>
          )}
        </div>
        {icon && (
          <div className="text-4xl opacity-20">
            {icon}
          </div>
        )}
      </div>
    </div>
  );
}

interface DonutChartProps {
  data: { label: string; value: number; percentage: number }[];
  title: string;
}

export function DonutChart({ data, title }: DonutChartProps) {
  const colors = [
    'rgb(59, 130, 246)',   // blue-600
    'rgb(16, 185, 129)',   // green-500
    'rgb(245, 158, 11)',   // amber-500
    'rgb(239, 68, 68)',    // red-500
    'rgb(139, 92, 246)',   // violet-500
    'rgb(236, 72, 153)',   // pink-500
    'rgb(14, 165, 233)',   // sky-500
    'rgb(34, 197, 94)',    // green-400
  ];

  const topData = data.slice(0, 8);
  const othersCount = data.slice(8).reduce((sum, item) => sum + item.value, 0);
  const othersPercentage = data.slice(8).reduce((sum, item) => sum + item.percentage, 0);
  
  const displayData = topData.length < data.length
    ? [...topData, { label: 'Others', value: othersCount, percentage: othersPercentage }]
    : topData;

  return (
    <div className="bg-white rounded-lg border border-gray-200 p-6">
      <h3 className="text-lg font-semibold text-gray-900 mb-4">{title}</h3>
      <div className="space-y-2">
        {displayData.map((item, index) => (
          <div key={index} className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <div
                className="w-4 h-4 rounded"
                style={{ backgroundColor: colors[index % colors.length] }}
              ></div>
              <span className="text-sm text-gray-700">{item.label}</span>
            </div>
            <span className="text-sm font-medium text-gray-900">
              {item.value} ({item.percentage.toFixed(1)}%)
            </span>
          </div>
        ))}
      </div>
    </div>
  );
}
