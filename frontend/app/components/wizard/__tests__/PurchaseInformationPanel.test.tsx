import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import PurchaseInformationPanel from "../panels/PurchaseInformationPanel";
import { EMPTY_FORM_DATA } from "../types";

describe("PurchaseInformationPanel", () => {
  const lookups = {
    loading: false,
    error: null,
    artists: [],
    labels: [],
    genres: [],
    countries: [],
    formats: [],
    packagings: [],
    stores: [
      { id: 1, name: "Rough Trade" },
      { id: 2, name: "Discogs Seller" },
    ],
  };

  it("shows previously added stores when the picker is opened and lets one be selected", async () => {
    const user = userEvent.setup();
    const onChange = jest.fn();

    render(
      <PurchaseInformationPanel
        data={EMPTY_FORM_DATA}
        onChange={onChange}
        errors={{}}
        lookups={lookups}
      />
    );

    await user.click(screen.getByLabelText(/store \/ seller/i));

    const savedStore = await screen.findByRole("button", { name: "Rough Trade" });
    expect(savedStore).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Discogs Seller" })).toBeInTheDocument();

    await user.click(savedStore);

    expect(onChange).toHaveBeenLastCalledWith({
      purchaseInfo: expect.objectContaining({
        storeId: 1,
        storeName: "Rough Trade",
      }),
    });
  });

  it("keeps allowing a brand new store to be typed", async () => {
    const user = userEvent.setup();
    const onChange = jest.fn();

    render(
      <PurchaseInformationPanel
        data={EMPTY_FORM_DATA}
        onChange={onChange}
        errors={{}}
        lookups={lookups}
      />
    );

    await user.type(screen.getByLabelText(/store \/ seller/i), "New Indie Shop");

    expect(onChange).toHaveBeenLastCalledWith({
      purchaseInfo: expect.objectContaining({
        storeId: undefined,
        storeName: "New Indie Shop",
      }),
    });
  });
});
