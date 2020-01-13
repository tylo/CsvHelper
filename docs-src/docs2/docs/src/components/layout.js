import React, { Component, createRef } from "react"
import Header from "./header"
import Footer from "./footer";

class Layout extends Component {

	render() {
		return (
			<>
				<Header />
				<div ref={this.bodyRef} className="body">
					{this.props.children}
				</div>
				<Footer />
			</>
		)
	}

}

export default Layout
