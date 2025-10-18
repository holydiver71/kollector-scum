import { render, screen } from '@testing-library/react';
import { StatCard, BarChart, LineChart, DonutChart } from '../StatisticsCharts';

describe('StatCard Component', () => {
  it('renders title and value correctly', () => {
    render(<StatCard title="Total Releases" value={2393} />);
    
    expect(screen.getByText('Total Releases')).toBeInTheDocument();
    expect(screen.getByText('2393')).toBeInTheDocument();
  });

  it('renders with icon when provided', () => {
    render(<StatCard title="Total Releases" value={2393} icon="💿" />);
    
    expect(screen.getByText('💿')).toBeInTheDocument();
  });

  it('renders subtitle when provided', () => {
    render(<StatCard title="Collection Value" value="£1,234" subtitle="Total purchase price" />);
    
    expect(screen.getByText('Total purchase price')).toBeInTheDocument();
  });

  it('renders positive trend correctly', () => {
    render(
      <StatCard 
        title="Total Releases" 
        value={2393} 
        trend={{ value: 5, isPositive: true }}
      />
    );
    
    expect(screen.getByText('↑')).toBeInTheDocument();
    expect(screen.getByText('5%')).toBeInTheDocument();
  });

  it('renders negative trend correctly', () => {
    render(
      <StatCard 
        title="Total Releases" 
        value={2393} 
        trend={{ value: 3, isPositive: false }}
      />
    );
    
    expect(screen.getByText('↓')).toBeInTheDocument();
    expect(screen.getByText('3%')).toBeInTheDocument();
  });
});

describe('BarChart Component', () => {
  const mockData = [
    { label: 'Metal', value: 500, percentage: 20.9 },
    { label: 'Rock', value: 450, percentage: 18.8 },
    { label: 'Jazz', value: 200, percentage: 8.4 }
  ];

  it('renders chart title', () => {
    render(<BarChart data={mockData} title="Top Genres" />);
    
    expect(screen.getByText('Top Genres')).toBeInTheDocument();
  });

  it('renders all data labels', () => {
    render(<BarChart data={mockData} title="Top Genres" />);
    
    expect(screen.getByText('Metal')).toBeInTheDocument();
    expect(screen.getByText('Rock')).toBeInTheDocument();
    expect(screen.getByText('Jazz')).toBeInTheDocument();
  });

  it('renders values and percentages', () => {
    render(<BarChart data={mockData} title="Top Genres" />);
    
    expect(screen.getByText('500 (20.9%)')).toBeInTheDocument();
    expect(screen.getByText('450 (18.8%)')).toBeInTheDocument();
  });

  it('respects maxBars limit', () => {
    const largeData = Array.from({ length: 20 }, (_, i) => ({
      label: `Genre ${i}`,
      value: 100 - i,
      percentage: 5 - i * 0.1
    }));
    
    render(<BarChart data={largeData} title="Top Genres" maxBars={5} />);
    
    expect(screen.getByText('Genre 0')).toBeInTheDocument();
    expect(screen.getByText('Genre 4')).toBeInTheDocument();
    expect(screen.queryByText('Genre 5')).not.toBeInTheDocument();
  });
});

describe('LineChart Component', () => {
  const mockYearData = [
    { year: 1980, count: 45 },
    { year: 1981, count: 52 },
    { year: 1982, count: 38 }
  ];

  it('renders chart title', () => {
    render(<LineChart data={mockYearData} title="Releases by Year" />);
    
    expect(screen.getByText('Releases by Year')).toBeInTheDocument();
  });

  it('displays year range', () => {
    render(<LineChart data={mockYearData} title="Releases by Year" />);
    
    expect(screen.getByText('Years: 1980 - 1982')).toBeInTheDocument();
  });

  it('handles empty data gracefully', () => {
    render(<LineChart data={[]} title="Releases by Year" />);
    
    expect(screen.getByText('No data available')).toBeInTheDocument();
  });

  it('renders tooltip data attributes', () => {
    const { container } = render(<LineChart data={mockYearData} title="Releases by Year" />);
    
    // Check that bars are rendered (tooltips appear on hover)
    const bars = container.querySelectorAll('[style*="height"]');
    expect(bars.length).toBeGreaterThan(0);
  });
});

describe('DonutChart Component', () => {
  const mockData = [
    { label: 'Vinyl', value: 1000, percentage: 41.8 },
    { label: 'CD', value: 800, percentage: 33.4 },
    { label: 'Cassette', value: 593, percentage: 24.8 }
  ];

  it('renders chart title', () => {
    render(<DonutChart data={mockData} title="Format Distribution" />);
    
    expect(screen.getByText('Format Distribution')).toBeInTheDocument();
  });

  it('renders all data labels and values', () => {
    render(<DonutChart data={mockData} title="Format Distribution" />);
    
    expect(screen.getByText('Vinyl')).toBeInTheDocument();
    expect(screen.getByText('CD')).toBeInTheDocument();
    expect(screen.getByText('Cassette')).toBeInTheDocument();
    expect(screen.getByText('1000 (41.8%)')).toBeInTheDocument();
  });

  it('groups items beyond 8 into "Others" category', () => {
    const largeData = Array.from({ length: 10 }, (_, i) => ({
      label: `Format ${i}`,
      value: 100 - i * 5,
      percentage: 10 - i * 0.5
    }));
    
    render(<DonutChart data={largeData} title="Format Distribution" />);
    
    expect(screen.getByText('Others')).toBeInTheDocument();
  });

  it('renders color indicators for each item', () => {
    const { container } = render(<DonutChart data={mockData} title="Format Distribution" />);
    
    const colorIndicators = container.querySelectorAll('[style*="background"]');
    expect(colorIndicators.length).toBe(mockData.length);
  });
});
